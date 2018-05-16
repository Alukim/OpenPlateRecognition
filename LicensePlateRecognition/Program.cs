using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LicensePlateRecognition
{
    class Program
    {
        // Computer Vision Api
        public const string SubscriptionKey = "f7ddd3fe9dc347b0a40bff1411a4326d";
        public const string UriBase = "https://northeurope.api.cognitive.microsoft.com/vision/v1.0/recognizeText";

        // constants for checkIfPossibleChar, this checks one possible char only (does not compare to another char)
        public const int MIN_PIXEL_WIDTH = 2;
        public const int MIN_PIXEL_HEIGHT = 8;

        public const double MIN_ASPECT_RATIO = 0.25;
        public const double MAX_ASPECT_RATIO = 1.0;

        public const int MIN_PIXEL_AREA = 80;

        // constants for comparing two chars
        public const double MIN_DIAG_SIZE_MULTIPLE_AWAY = 0.3;
        public const double MAX_DIAG_SIZE_MULTIPLE_AWAY = 5.0;

        public const double MAX_CHANGE_IN_AREA = 0.5;

        public const double MAX_CHANGE_IN_WIDTH = 0.8;
        public const double MAX_CHANGE_IN_HEIGHT = 0.2;

        public const double MAX_ANGLE_BETWEEN_CHARS = 12.0;

        // other constants
        public const int MIN_NUMBER_OF_MATCHING_CHARS = 3;

        public const int RESIZED_CHAR_IMAGE_WIDTH = 20;
        public const int RESIZED_CHAR_IMAGE_HEIGHT = 30;

        public const int MIN_CONTOUR_AREA = 100;

        // Extract plate
        public const double PLATE_WIDTH_PADDING_FACTOR = 1.3;
        public const double PLATE_HEIGHT_PADDING_FACTOR = 1.5;

        public static Scalar ScalarBlack = new Scalar(0, 0, 0);
        public static Scalar ScalarWhite = new Scalar(255.0, 255.0, 255.0);
        public static Scalar ScalarBlue = new Scalar(255.0, 0.0, 0.0);

        public static Random random = new Random();

        static void Main(string[] args)
        {
            var images = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."), "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg"));

            foreach (var image in images)
            {
                MainFunction(image);
            }

            Console.ReadLine();
            Console.ReadLine();
        }

        private static void MainFunction(string imageUrl)
        {
            // First step is to load new image.
            var image = ImageRepository.LoadImage(imageUrl);
            var imageNameWithoutExtension = Path.GetFileNameWithoutExtension(imageUrl);
            //Cv2.ImShow("Original", image);
            //Cv2.WaitKey(0);
            // Next we must do some preprocessing (From RGB image, to GrayScale, Blur, threshold). Also we can do some magic with contrast. Maybe maximize constrast with top hat and black hat. I don't know. 
            var plates = DetectPlates.DetectPlatesInScene(image);
            ResizePlatesIfNeeded(plates);
            var platesWithChars = DetectChars.DetectCharsInPlates(plates).ToList();

            var filtered = FilterLicencePlates(platesWithChars);
            LogResult(imageNameWithoutExtension, filtered);

            if(filtered.Any())
            {
                DrawRedRectangleAroundPlates(image, filtered);                // draw red rectangle around plate
                image = ResizeFinalImageIfNeeded(image);
                Cv2.ImShow("Recognized", image);
                Cv2.WaitKey(0);
            }
            
            Console.WriteLine();

            for(int i = 0; i < platesWithChars.Count; ++i)
            {
                try
                {
                    File.Delete($"{i.ToString()}.png");                    
                }
                catch (System.Exception)
                {
                }
            }
        }

        private static Mat ResizeFinalImageIfNeeded(Mat image)
        {
            var result = image;
            if(image.Height > 1000)
            {
                var deriv = 1000f / image.Height;
                var height = image.Height * deriv;
                var width = image.Width * deriv;
                result = image.Resize(new Size(width, height));
            }
            return result;
        }

        private static void ResizePlatesIfNeeded(List<PossiblePlate> plates)
        {
            foreach (var plate in plates)
            {
                if(plate.ImgPlate.Height < 40)
                {
                    var deriv = 40f / plate.ImgPlate.Height;
                    var newWidth = plate.ImgPlate.Width * deriv;
                    var newHeight = plate.ImgPlate.Height * deriv;
                    plate.ImgPlate = plate.ImgPlate.Resize(new Size(newWidth, newHeight));
                }
            }
        }

        public static void DrawRedRectangleAroundPlates(Mat imgOriginalScene, List<PossiblePlate> licPlates)
        {
            foreach (var plate in licPlates)
            {
                var p2fRectPoints = plate.RrLocationOfPlateInScene.Points();            // get 4 vertices of rotated rect

                for (int i = 0; i < 4; i++)
                {                                       // draw 4 red lines
                    Cv2.Line(imgOriginalScene, p2fRectPoints[i], p2fRectPoints[(i + 1) % 4], ScalarBlue, 2);
                }
            }
        }

        public static List<PossiblePlate> FilterLicencePlates(List<PossiblePlate> plates)
        {
            var result = new List<PossiblePlate>();
            foreach (var plate in plates)
            {
                if(string.IsNullOrWhiteSpace(plate.StrChars))
                    continue;
                if(plate.StrChars.Count() < 5)
                    continue;
                result.Add(plate);
            }
            return result;
        }

        public static void LogResult(string imageName, List<PossiblePlate> plates)
        {
            Console.WriteLine($"Image name: {imageName}");
            Console.WriteLine($"Expected result: {imageName.ToBoolean().ToResult()}");
            if(!plates.Any())
                Console.WriteLine("Actual result: Not founded");
            else
            {
                Console.WriteLine("Actual result: Founded");
                foreach (var plate in plates)
                {
                    Console.WriteLine(plate.StrChars);
                }
            }
        }
    }
}