using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace LicensePlateRecognition
{
    public class HttpClientProvider
    {
        private HttpClientProvider()
        {
        }

        private static HttpClient instance;

        public static HttpClient GetInstance()
        {
            if (instance == null)
            {
                instance = new HttpClient();
                instance.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Program.SubscriptionKey);
            }
            return instance;
        }
    }
}
