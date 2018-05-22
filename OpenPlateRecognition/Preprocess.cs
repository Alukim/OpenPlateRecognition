using OpenCvSharp;

namespace LicensePlateRecognition
{
    public static class Preprocess
    {
        private static Size GAUSSIAN_SMOOTH_FILTER_SIZE = new Size(5, 5);
        private const int ADAPTIVE_THRESH_BLOCK_SIZE = 19;
        private const int ADAPTIVE_THRESH_WEIGHT = 9;

        public static Mat ImagePreprocessing(this Mat image)
            => image
                .GrayScale()
                .MaximizeContrast()
                .GaussianBlur()
                .Threshold();

        public static Mat GrayScale(this Mat image)
        {
            var imgHSV = new Mat();
            Cv2.CvtColor(image, imgHSV, ColorConversionCodes.BGR2HSV);
            Cv2.Split(imgHSV, out Mat[] vectorOfHSVImages);
            return vectorOfHSVImages[2];
        }

        public static Mat GaussianBlur(this Mat image)
        {
            var blur = new Mat();
            Cv2.GaussianBlur(image, blur, GAUSSIAN_SMOOTH_FILTER_SIZE, 0);
            return blur;
        }

        public static Mat Threshold(this Mat image)
        {
            var thresh = new Mat();
            Cv2.AdaptiveThreshold(
                src: image,
                dst: thresh,
                maxValue: 255.0,
                adaptiveMethod: AdaptiveThresholdTypes.GaussianC,
                thresholdType: ThresholdTypes.BinaryInv,
                blockSize: ADAPTIVE_THRESH_BLOCK_SIZE,
                c: ADAPTIVE_THRESH_WEIGHT);
            return thresh;
        }

        public static Mat MaximizeContrast(this Mat imgGrayscale)
        {
            var imgTopHat = new Mat();
            var imgBlackHat = new Mat();
            var imgGrayscalePlusTopHat = new Mat();
            var imgGrayscalePlusTopHatMinusBlackHat = new Mat();

            var structuringElement = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));

            Cv2.MorphologyEx(imgGrayscale, imgTopHat, MorphTypes.TopHat, structuringElement);
            Cv2.MorphologyEx(imgGrayscale, imgBlackHat, MorphTypes.BlackHat, structuringElement);

            imgGrayscalePlusTopHat = imgGrayscale + imgTopHat;
            imgGrayscalePlusTopHatMinusBlackHat = imgGrayscalePlusTopHat - imgBlackHat;

            return imgGrayscalePlusTopHatMinusBlackHat;
        }
    }
}
