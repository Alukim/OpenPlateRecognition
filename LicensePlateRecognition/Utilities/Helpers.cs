namespace LicensePlateRecognition.Utilities
{
    public static class Helpers
    {
        public static bool ToBoolean(this string imageName) => imageName.Contains("true") ? true : false;
        public static string ToResult(this bool expectedResult) => expectedResult ? "Founded" : "Not founded";
    }
}