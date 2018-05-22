using LicensePlateRecognition.ExternalDependencies.Entities;
using LicensePlateRecognition.ExternalDependencies.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LicensePlateRecognition
{
    public class CognitiveServiceHttpClientProvider
    {
        private const string requestParameters = "handwriting=true";

        private readonly HttpClient client;

        public CognitiveServiceHttpClientProvider()
        {
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders
                .Add("Ocp-Apim-Subscription-Key", Program.SubscriptionKey);
        }

        private string GetUri()
        {
            string uri = Program.UriBase;
            if (!uri.EndsWith("?"))
                uri = $"{uri}?";

            return $"{uri}{requestParameters}";
        }

        private byte[] GetImageAsByteArray(string imageFilePath)
        {
            var fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        public async Task<string> MakeAnalysisRequest(string imageFilePath)
        { 
            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(GetUri(), content);
            }

            string contentString = "";
            if (response.IsSuccessStatusCode)
            {
                var operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return string.Empty;
                }
            }
            else
            {
                Console.WriteLine($"{imageFilePath}");
                Console.WriteLine("\nError:\n");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                Console.WriteLine();
                return string.Empty;
            }

            var responseObject = JsonConvert.DeserializeObject<RootObject>(contentString);

            return responseObject.GetResponse();
        }
    }
}
