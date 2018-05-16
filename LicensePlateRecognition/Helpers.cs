
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LicensePlateRecognition
{
    public static class Helpers
    {
        public static bool ToBoolean(this string imageName) => imageName.Contains("true") ? true : false;
        public static string ToResult(this bool expectedResult) => expectedResult ? "Founded" : "Not founded";

    }
}