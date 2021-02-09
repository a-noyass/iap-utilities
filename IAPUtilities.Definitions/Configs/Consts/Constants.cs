// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.IAPUtilities.Definitions.Configs.Consts
{
    public class Constants
    {
        // text analytics
        public const int TextAnalyticsPredictionMaxCharLimit = 5000;
        public const int TextAnaylticsApiCallDocumentLimit = 5;
        public const string TextAnalyticsLanguageCode = "en";

        public const long RetryMaxWaitTimeInMillis = 1000;
        public const int MaxRetries = 10;
    }
}