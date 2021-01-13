using IAPUtilities.SDK;
using Microsoft.IAPUtilities.Definitions.Enums.IAP;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace IAPUtilities.Tests
{
    public class SDKTest1
    {
        private readonly Client _client;

        public SDKTest1()
        {
            string endpoint = Environment.GetEnvironmentVariable("LUIS_PREDICTION_ENDPOINT");
            string key = Environment.GetEnvironmentVariable("LUIS_PREDICTION_KEY");
            string appId = Environment.GetEnvironmentVariable("LUIS_APP_ID");

            _client = new Client(endpoint, key, appId);
        }

        [Fact]
        public async Task Test1Async()
        {
            var fs = File.OpenRead(@"TestData\Transcript1.txt");
            var result = await _client.RunAsync(fs);
            var expected = File.ReadAllText(@"C:\Users\v-noyasser\Desktop\iap-utilities\IAPUtilities.Tests\TestData\Transcript1Result.json");

            Assert.Equal(ChannelType.Agent, result.Meta.Channel);
            Assert.Equal("29526270", result.Meta.TranscriptId);
            Assert.Equal(7, result.Conversation.Count);
        }
    }
}
