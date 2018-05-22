using System.Collections.Generic;

namespace LicensePlateRecognition.ExternalDependencies.Entities
{
    public class Word
    {
        public List<int> BoundingBox { get; set; }
        public string Text { get; set; }
    }
}
