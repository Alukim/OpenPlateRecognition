﻿using System.Collections.Generic;

namespace LicensePlateRecognition.ExternalDependencies.Entities
{
    public class Line
    {
        public List<int> BoundingBox { get; set; }
        public string Text { get; set; }
        public List<Word> Words { get; set; }
    }
}
