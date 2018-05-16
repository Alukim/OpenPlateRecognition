using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LicensePlateRecognition
{
    public static class DetectChars
    {
        public static List<PossiblePlate> DetectCharsInPlates(List<PossiblePlate> listOfPossiblePlates)
        {
            var recognizedPlates = new List<PossiblePlate>();
            //var results = new TaskFactory().StartNew(() => MakeAnalysisRequests(listOfPossiblePlates)).GetAwaiter().GetResult().Result;
            //foreach (var result in results)
            //{
            //    var fileName = result.Key;
            //    var recognizedString = result.Value.recognizedString;
            //    var possiblePlate = result.Value.plate;
            //     if (!string.IsNullOrEmpty(recognizedString))
            //        recognizedPlates.Add(new PossiblePlate
            //        {
            //            ImgPlate = possiblePlate.ImgPlate,
            //            StrChars = recognizedString,
            //            RrLocationOfPlateInScene = possiblePlate.RrLocationOfPlateInScene
            //        });
            //}
            for (int i = 0; i < listOfPossiblePlates.Count; i++)
            {
                var fileName = $"{i.ToString()}.png";
                var possiblePlate = listOfPossiblePlates[i];
                possiblePlate.ImgPlate.SaveImage(fileName);
                var recognizedString = MakeAnalysisRequest(fileName).Result;
                if (!string.IsNullOrEmpty(recognizedString))
                    recognizedPlates.Add(new PossiblePlate
                    {
                        ImgPlate = possiblePlate.ImgPlate,
                        StrChars = recognizedString,
                        RrLocationOfPlateInScene = possiblePlate.RrLocationOfPlateInScene
                    });
            }

            return recognizedPlates;
        }

        public static async Task<Dictionary<string, (string recognizedString, PossiblePlate plate)>> MakeAnalysisRequests(List<PossiblePlate> listOfPossiblePlates)
        {
            var tasks = new Dictionary<string, Task<string>>();
            var plates = new Dictionary<string, PossiblePlate>();
            for (int i = 0; i < listOfPossiblePlates.Count; i++)
            {
                var fileName = $"{i.ToString()}.png";
                var possiblePlate = listOfPossiblePlates[i];
                possiblePlate.ImgPlate.SaveImage(fileName);
                tasks.Add(fileName, MakeAnalysisRequest(fileName));
                plates.Add(fileName, possiblePlate);
                Thread.Sleep(100);
            }
            await Task.WhenAll(tasks.Values);
            return tasks.ToDictionary(x => x.Key, x => (x.Value.Result, plates[x.Key]));
        }

        /// <summary>
        /// Gets the analysis of the specified image file by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        static async Task<string> MakeAnalysisRequest(string imageFilePath)
        {
            var client = HttpClientProvider.GetInstance();

            // Request headers.
            //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Program.SubscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "handwriting=true";

            // Assemble the URI for the REST API Call.
            string uri = Program.UriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);
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
                    //Console.WriteLine("\nTimeout error.\n");
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

            return GetString(responseObject);
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            var fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        private static string GetString(RootObject root)
        {
            string text = string.Empty;
            try
            {
                var words = root.recognitionResult.lines.SelectMany(x => x.words.Select(z => z.text)).ToList();
                text = String.Join("", words);
                if (text.Length > 8)
                    text = string.Empty;
            }
            catch(Exception e)
            {
                text = string.Empty;
            }
            return text;
        }

        public class Word
        {
            public List<int> boundingBox { get; set; }
            public string text { get; set; }
        }

        public class Line
        {
            public List<int> boundingBox { get; set; }
            public string text { get; set; }
            public List<Word> words { get; set; }
        }

        public class RecognitionResult
        {
            public List<Line> lines { get; set; }
        }

        public class RootObject
        {
            public string status { get; set; }
            public bool succeeded { get; set; }
            public bool failed { get; set; }
            public bool finished { get; set; }
            public RecognitionResult recognitionResult { get; set; }
        }
    }
}
