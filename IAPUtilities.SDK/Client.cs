using Azure.AI.TextAnalytics;
using Microsoft.IAPUtilities.Core.Services.IAP;
using Microsoft.IAPUtilities.Core.Services.Luis;
using Microsoft.IAPUtilities.Core.Services.TextAnalytics;
using Microsoft.IAPUtilities.Definitions.APIs.Services;
using Microsoft.IAPUtilities.Definitions.Configs.Consts;
using Microsoft.IAPUtilities.Definitions.Models.IAP;
using Microsoft.IAPUtilities.Definitions.Models.Luis;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IAPUtilities.SDK
{
    public class Client
    {
        private ITranscriptParser _transcriptParser;
        private ILuisPredictionService _luisPredictionService;
        private IIAPResultGenerator _resultGenerator;
        private ITextAnalyticsService _textAnalyticsService;

        public Client(string luisEndpoint, string luisKey, string luisAppId)
        {
            _luisPredictionService = new LuisPredictionService(luisEndpoint, luisKey, luisAppId);
            _transcriptParser = new TranscriptParser();
            _resultGenerator = new IAPResultGenerator();
        }
        public Client(string luisEndpoint, string luisKey, string luisAppId, string textAnalyticsEndpoint, string textAnalyticsKey, string language = Constants.TextAnalyticsLanguageCode)
        {
            _luisPredictionService = new LuisPredictionService(luisEndpoint, luisKey, luisAppId);
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
            var tasks = transcript.Utterances.Select(async utterance =>
            {
                // run luis prediction endpoint
                luisDictionary[utterance.Timestamp] = await _luisPredictionService.Predict(utterance.Text);
                // run TA prediction endpoint
                if (enableTA)
                {
                    textAnalyticsDictionary[utterance.Timestamp] = await _textAnalyticsService.PredictSentimentAsync(utterance.Text, opinionMining: true);
                }
            });
            await Task.WhenAll(tasks);

            // concatenate result
            return _resultGenerator.GenerateResult(luisDictionary, textAnalyticsDictionary, transcript.Channel, transcript.Id);
        }
    }
}
