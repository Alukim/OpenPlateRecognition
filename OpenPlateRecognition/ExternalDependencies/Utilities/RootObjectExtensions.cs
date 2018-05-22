using LicensePlateRecognition.ExternalDependencies.Entities;
using System;
using System.Linq;

namespace LicensePlateRecognition.ExternalDependencies.Utilities
{
    public static class RootObjectExtensions
    {
        public static string GetResponse(this RootObject root)
        {
            string text = string.Empty;
            try
            {
                var words = root.RecognitionResult.Lines.SelectMany(x => x.Words.Select(z => z.Text)).ToList();
                text = String.Join("", words);
                if (text.Length > 8)
                    text = string.Empty;
            }
            catch (Exception)
            {
                text = string.Empty;
            }
            return text;
        }
    }
}
