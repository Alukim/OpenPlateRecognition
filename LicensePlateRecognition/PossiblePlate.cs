using OpenCvSharp;

namespace LicensePlateRecognition
{
    public class PossiblePlate
    {
        public PossiblePlate() { }

        public PossiblePlate(Mat imgPlate, Mat imgGrayscale, Mat imgThresh, RotatedRect rrLocationOfPlateInScene, string strChars)
        {
            ImgPlate = imgPlate;
            ImgGrayscale = imgGrayscale;
            ImgThresh = imgThresh;
            RrLocationOfPlateInScene = rrLocationOfPlateInScene;
            StrChars = strChars;
        }

        public Mat ImgPlate { get; set; }
        public Mat ImgGrayscale { get; set; }
        public Mat ImgThresh { get; set; }
        public RotatedRect RrLocationOfPlateInScene { get; set; }
        public string StrChars { get; set; }

        public bool SortDescendingByNumberOfChars(PossiblePlate possiblePlate)
            => this.StrChars.Length > possiblePlate.StrChars.Length;
    }
}
