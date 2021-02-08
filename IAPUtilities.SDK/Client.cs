using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.IAPUtilities.Core.Services.IAP;
using Microsoft.IAPUtilities.Core.Services.Luis;
using Microsoft.IAPUtilities.Core.Services.TextAnalytics;
using Microsoft.IAPUtilities.Definitions.APIs.Services;
using Microsoft.IAPUtilities.Definitions.Configs.Consts;
using Microsoft.IAPUtilities.Definitions.Models.IAP;
using Microsoft.IAPUtilities.Definitions.Models.Luis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IAPUtilities.SDK
{
    public class Client
    {
        private readonly ITranscriptParser _transcriptParser;
        private readonly IIAPResultGenerator _resultGenerator;
        private readonly ITextAnalyticsService _textAnalyticsService;
        private readonly List<LuisPredictionService> _luisPredictionServices;

        public Client(string luisEndpoint, string luisKey, string luisAppId)
        {
            _luisPredictionServices = new List<LuisPredictionService>()
            {
                new LuisPredictionService(luisEndpoint, luisKey, luisAppId)
            };
            _transcriptParser = new TranscriptParser();
            _resultGenerator = new IAPResultGenerator();
        }

        public Client(List<LuisCredentials> credentials)
        {
            if (credentials == null || credentials.Count == 0)
            {
                throw new Exception("Credentials list can't be null or empty");
            }
            _luisPredictionServices = new List<LuisPredictionService>();
            foreach (var credential in credentials)
            {
                _luisPredictionServices.Add(new LuisPredictionService(credential.Endpoint, credential.Key, credential.AppId));
            }
            _transcriptParser = new TranscriptParser();
            _resultGenerator = new IAPResultGenerator();
        }

        public Client(string luisEndpoint, string luisKey, string luisAppId, string textAnalyticsEndpoint, string textAnalyticsKey, string language = Constants.TextAnalyticsLanguageCode)
        {
            _luisPredictionServices = new List<LuisPredictionService>()
            {
                new LuisPredictionService(luisEndpoint, luisKey, luisAppId)
            };
            _transcriptParser = new TranscriptParser();
            _resultGenerator = new IAPResultGenerator();
            _textAnalyticsService = new TextAnalyticsService(textAnalyticsEndpoint, textAnalyticsKey, language);
        }

        public async Task<ResultTranscript> RunAsync(Stream file, bool enableTA = false)
        {
            //  parse file (extract utterances)
            var transcript = await _transcriptParser.ParseTranscriptAsync(file);

            var luisDictionary = new ConcurrentDictionary<long, CustomLuisResponse>();
            var textAnalyticsDictionary = new ConcurrentDictionary<long, DocumentSentiment>();
            var tasks = transcript.Utterances.Select(async (utterance, index) =>
            {
                await GetLuisResponse(utterance, luisDictionary, index);
                if (enableTA)
                {
                    await GetTextAnalyticsResponse(utterance, textAnalyticsDictionary);
                }
            });
            await Task.WhenAll(tasks);

            // concatenate result
            return _resultGenerator.GenerateResult(luisDictionary, textAnalyticsDictionary, transcript.Channel, transcript.Id);
        }

        private async Task GetTextAnalyticsResponse(ConversationUtterance utterance, ConcurrentDictionary<long, DocumentSentiment> textAnalyticsDictionary)
        {
            var sent = false;
            while (!sent)
            {
                try
                {
                    // run TA prediction endpoint
                    textAnalyticsDictionary[utterance.Timestamp] = await _textAnalyticsService.PredictSentimentAsync(utterance.Text, opinionMining: true);
                    sent = true;
                }
                catch (RequestFailedException e)
                {
                    if (e.Status == (int)HttpStatusCode.TooManyRequests)
                    {
                        await WaitRandomTime(Constants.RetryMaxWaitTimeInMillis);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task GetLuisResponse(ConversationUtterance utterance, ConcurrentDictionary<long, CustomLuisResponse> luisDictionary, int index)
        {
            var sent = false;
            while (!sent)
            {
                try
                {
                    // run luis prediction endpoint
                    int clientIndex = index % _luisPredictionServices.Count;
                    luisDictionary[utterance.Timestamp] = await _luisPredictionServices[clientIndex].Predict(utterance.Text);
                    sent = true;
                }
                catch (ErrorException e)
                {
                    if (e.Response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await WaitRandomTime(Constants.RetryMaxWaitTimeInMillis);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private async Task WaitRandomTime(long MaxWaitTimeInMillis)
        {
            Random r = new Random();
            var delay = new TimeSpan((long)(r.NextDouble() * 1000));
            await Task.Delay(delay);
        }
    }
}
