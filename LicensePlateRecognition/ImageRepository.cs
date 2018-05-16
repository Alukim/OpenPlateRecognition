using OpenCvSharp;

namespace LicensePlateRecognition
{
    public static class ImageRepository
    {
        public static Mat LoadImage(string fileName)
            => Cv2.ImRead(fileName);
    }
}
