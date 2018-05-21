using System;

namespace LicensePlateRecognition.ExternalDependencies.Entities
{
    public class RootObject
    {
        public string Status { get; set; }
        public bool Succeeded { get; set; }
        public bool Failed { get; set; }
        public bool Finished { get; set; }
        public RecognitionResult RecognitionResult { get; set; }

        internal static string GetString()
        {
            throw new NotImplementedException();
        }
    }
}
