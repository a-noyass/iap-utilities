using IAPUtilities.SDK;
using Microsoft.IAPUtilities.Definitions.Enums.IAP;
using Microsoft.IAPUtilities.Definitions.Models.Luis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace IAPUtilities.Tests
{
    public class SDKTest1
    {
        private readonly string Endpoint;
        private readonly string Key;
        private readonly string AppId;

        public SDKTest1()
        {
            Endpoint = Environment.GetEnvironmentVariable("LUIS_PREDICTION_ENDPOINT");
            Key = Environment.GetEnvironmentVariable("LUIS_PREDICTION_KEY");
            AppId = Environment.GetEnvironmentVariable("LUIS_APP_ID");
        }

        [Fact]
        public async Task Test1Async()
        {
            var client = new Client(Endpoint, Key, AppId);

            var fs = File.OpenRead(@"TestData\Transcript1.txt");
            var result = await client.RunAsync(fs);

            Assert.Equal(ChannelType.Agent, result.Meta.Channel);
            Assert.Equal("29526270", result.Meta.TranscriptId);
            Assert.Equal(7, result.Conversation.Count);
        }

        [Fact]
        public async Task Test2Async()
        {
            var credentials = new List<LuisCredentials>()
            {
                new LuisCredentials(Endpoint, Key, AppId),
                new LuisCredentials(Endpoint, Key, AppId),
                new LuisCredentials(Endpoint, Key, AppId),
                new LuisCredentials(Endpoint, Key, AppId)
            };
            var client = new Client(credentials);
            var fs = File.OpenRead(@"TestData\Transcript1.txt");
            var result = await client.RunAsync(fs);

            Assert.Equal(ChannelType.Agent, result.Meta.Channel);
            Assert.Equal("29526270", result.Meta.TranscriptId);
            Assert.Equal(7, result.Conversation.Count);
        }
    }
}
