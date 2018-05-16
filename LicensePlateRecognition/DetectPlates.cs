using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LicensePlateRecognition
{
    public static class DetectPlates
    {
        public static RNG rng = Cv2.TheRNG();

        public static List<PossiblePlate> DetectPlatesInScene(this Mat image)
        {
            var preprocessedImage = image.ImagePreprocessing();
            //Cv2.ImShow("Preprocessed image", preprocessedImage);

            // Find possible chars (In this step, first we found all contours on Threshold image next, we doing some first filtering of chars `CheckIfPossibleChar`)
            var listOfPossibleCharsInScene = FindPossibleCharsInScene(preprocessedImage);

            //Ok, now we have list of possible chars on scene. We want to group it into List of List of PossibleChars. 
            var groupedListOfPossibleChars = FindListOfGroupOfMatchingChars(listOfPossibleCharsInScene);

            //Console.WriteLine($"step 3 - vectorOfVectorsOfMatchingCharsInScene.size() = {groupedListOfPossibleChars.Count}");
            // 13 with MCLRNF1 image

            var imgContours = new Mat(image.Size(), MatType.CV_8UC3, Program.ScalarBlack);
            var listOfPossiblePlates = new List<PossiblePlate>();

            foreach (var vectorOfMatchingChars in groupedListOfPossibleChars)
            {
                var contours = vectorOfMatchingChars.Select(x => x.Contour.ToList());

                Cv2.DrawContours(
                    image: imgContours,
                    contours: contours,
                    contourIdx: -1,
                    color: new Scalar(NextRandomRGB(), NextRandomRGB(), NextRandomRGB()));

                var possiblePlate = image.ExtractPlate(vectorOfMatchingChars);
                if (!possiblePlate.ImgPlate.Empty())
                    listOfPossiblePlates.Add(possiblePlate);
            }

            //Console.WriteLine($"{listOfPossiblePlates.Count()} possible plates found.");

            //Cv2.ImShow("Group of countours", imgContours);

            for (var i = 0; i < listOfPossiblePlates.Count; ++i)
            {
                var p2fRectPoints = listOfPossiblePlates[i].RrLocationOfPlateInScene.Points();

                for (int j = 0; j < 4; j++)
                {
                    Cv2.Line(imgContours, p2fRectPoints[j], p2fRectPoints[(j + 1) % 4], Program.ScalarBlue, 2);
                }
                //Cv2.ImShow("4a", imgContours);

                //Cv2.ImShow("4b", listOfPossiblePlates[i].ImgPlate);
                //Cv2.WaitKey(0);
            }
            //std::cout << std::endl << "plate detection complete, click on any image and press a key to begin char recognition . . ." << std::endl << std::endl;
            //Cv2.ImShow("Founded plates", imgContours);
            //Cv2.ImShow("Plate", listOfPossiblePlates.First().ImgPlate);
            //Cv2.WaitKey(0);

            return listOfPossiblePlates;
        }

        public static PossiblePlate ExtractPlate(this Mat originalImage, ICollection<PossibleChar> listOfMatchingChars)
        {
            var sortedList = listOfMatchingChars.OrderBy(x => x.IntCenterX);
            var lastMatchingChar = sortedList.Last();
            var firstMatchingChar = sortedList.First();

            float dblPlateCenterX = (float)(firstMatchingChar.IntCenterX + lastMatchingChar.IntCenterX) / 2.0f;
            float dblPlateCenterY = (float)(firstMatchingChar.IntCenterY + lastMatchingChar.IntCenterY) / 2.0f;

            var p2dPlateCenter = new Point2f(dblPlateCenterX, dblPlateCenterY);

            // calculate plate width and height
            int intPlateWidth = (int)((lastMatchingChar.BoundingRect.X + lastMatchingChar.BoundingRect.Width - firstMatchingChar.BoundingRect.X) * Program.PLATE_WIDTH_PADDING_FACTOR);

            double intPlateHeight = listOfMatchingChars.Average(x => x.BoundingRect.Height) * Program.PLATE_HEIGHT_PADDING_FACTOR;

            // calculate correction angle of plate region
            double dblOpposite = lastMatchingChar.IntCenterY - firstMatchingChar.IntCenterY;
            double dblHypotenuse = DistanceBetweenChars(firstMatchingChar, lastMatchingChar);
            double dblCorrectionAngleInRad = Math.Asin(dblOpposite / dblHypotenuse);
            double dblCorrectionAngleInDeg = dblCorrectionAngleInRad * (180.0 / Cv2.PI);

            // assign rotated rect member variable of possible plate
            var rrLocationOfPlateInScene = new RotatedRect(p2dPlateCenter, new Size2f((float)intPlateWidth, (float)intPlateHeight), (float)dblCorrectionAngleInDeg);

            // final steps are to perform the actual rotation
            var imgRotated = new Mat();
            var imgCropped = new Mat();

            var rotationMatrix = Cv2.GetRotationMatrix2D(p2dPlateCenter, dblCorrectionAngleInDeg, 1.0);         // get the rotation matrix for our calculated correction angle

            Cv2.WarpAffine(originalImage, imgRotated, rotationMatrix, originalImage.Size());            // rotate the entire image

            // crop out the actual plate portion of the rotated image
            Cv2.GetRectSubPix(imgRotated, new Size(rrLocationOfPlateInScene.Size.Width, rrLocationOfPlateInScene.Size.Height), rrLocationOfPlateInScene.Center, imgCropped);

            // copy the cropped plate image into the applicable member variable of the possible plate

            return new PossiblePlate
            {
                RrLocationOfPlateInScene = rrLocationOfPlateInScene,
                ImgPlate = imgCropped
            };
        }

        public static double NextRandomRGB()
        {
            return rng.Uniform(0, 256);
        }

        public static bool CheckIfPossibleChar(PossibleChar possibleChar)
            => possibleChar.BoundingRectArea > Program.MIN_PIXEL_AREA &&
                possibleChar.BoundingRect.Width > Program.MIN_PIXEL_WIDTH &&
                possibleChar.BoundingRect.Height > Program.MIN_PIXEL_HEIGHT &&
                possibleChar.DblAspectRatio > Program.MIN_ASPECT_RATIO &&
                possibleChar.DblAspectRatio < Program.MAX_ASPECT_RATIO;

        public static List<List<PossibleChar>> FindListOfGroupOfMatchingChars(ICollection<PossibleChar> listOfPossibleCharsInScene)
        {
            var vectorOfVectorsOfMatchingChars = new List<List<PossibleChar>>();

            foreach (var possibleChar in listOfPossibleCharsInScene)
            {
                var vectorOfMatchingChars = FindGroupOfMatchingChars(possibleChar, listOfPossibleCharsInScene);

                vectorOfMatchingChars.Add(possibleChar);          // also add the current char to current possible vector of matching chars

                // if current possible vector of matching chars is not long enough to constitute a possible plate
                if (vectorOfMatchingChars.Count < Program.MIN_NUMBER_OF_MATCHING_CHARS)
                {
                    continue;                       // jump back to the top of the for loop and try again with next char, note that it's not necessary
                                                    // to save the vector in any way since it did not have enough chars to be a possible plate
                }
                // if we get here, the current vector passed test as a "group" or "cluster" of matching chars
                vectorOfVectorsOfMatchingChars.Add(vectorOfMatchingChars.ToList());            // so add to our vector of vectors of matching chars

                // remove the current vector of matching chars from the big vector so we don't use those same chars twice,
                // make sure to make a new big vector for this since we don't want to change the original big vector
                var vectorOfPossibleCharsWithCurrentMatchesRemoved = new List<PossibleChar>();

                foreach (var possChar in listOfPossibleCharsInScene)
                {
                    if (vectorOfMatchingChars.FirstOrDefault(x => x.Equals(possChar)) == null)
                        vectorOfPossibleCharsWithCurrentMatchesRemoved.Add(possChar);
                }
                // declare new vector of vectors of chars to get result from recursive call
                var recursiveVectorOfVectorsOfMatchingChars = new List<List<PossibleChar>>();

                // recursive call
                recursiveVectorOfVectorsOfMatchingChars = FindListOfGroupOfMatchingChars(vectorOfPossibleCharsWithCurrentMatchesRemoved);   // recursive call !!

                vectorOfVectorsOfMatchingChars.AddRange(recursiveVectorOfVectorsOfMatchingChars);

                break;
            }

            return vectorOfVectorsOfMatchingChars;
        }

        public static ICollection<PossibleChar> FindGroupOfMatchingChars(PossibleChar possibleChar, ICollection<PossibleChar> listOfpossibleChars)
        {
            var vectorOfMatchingChars = new List<PossibleChar>();

            foreach (var possibleMatchingChar in listOfpossibleChars)
            {
                if (possibleMatchingChar.Equals(possibleChar))
                {
                    // then we should not include it in the vector of matches b/c that would end up double including the current char
                    continue;           // so do not add to vector of matches and jump back to top of for loop
                }

                // compute stuff to see if chars are a match
                double dblDistanceBetweenChars = DistanceBetweenChars(possibleChar, possibleMatchingChar);
                double dblAngleBetweenChars = AngleBetweenChars(possibleChar, possibleMatchingChar);
                double dblChangeInArea = (double)Math.Abs(possibleMatchingChar.BoundingRectArea - possibleChar.BoundingRectArea) / (double)possibleChar.BoundingRectArea;
                double dblChangeInWidth = (double)Math.Abs(possibleMatchingChar.BoundingRect.Width - possibleChar.BoundingRect.Width) / (double)possibleChar.BoundingRect.Width;
                double dblChangeInHeight = (double)Math.Abs(possibleMatchingChar.BoundingRect.Height - possibleChar.BoundingRect.Height) / (double)possibleChar.BoundingRect.Height;

                // check if chars match
                if (dblDistanceBetweenChars < (possibleChar.DblDiagonalSize * Program.MAX_DIAG_SIZE_MULTIPLE_AWAY) &&
                    dblAngleBetweenChars < Program.MAX_ANGLE_BETWEEN_CHARS &&
                    dblChangeInArea < Program.MAX_CHANGE_IN_AREA &&
                    dblChangeInWidth < Program.MAX_CHANGE_IN_WIDTH &&
                    dblChangeInHeight < Program.MAX_CHANGE_IN_HEIGHT)
                {
                    vectorOfMatchingChars.Add(possibleMatchingChar);      // if the chars are a match, add the current char to vector of matching chars
                }
            }

            return vectorOfMatchingChars;
        }

        public static double DistanceBetweenChars(PossibleChar firstChar, PossibleChar secondChar)
        {
            int intX = Math.Abs(firstChar.IntCenterX - secondChar.IntCenterX);
            int intY = Math.Abs(firstChar.IntCenterY - secondChar.IntCenterY);

            return (Math.Sqrt(Math.Pow(intX, 2) + Math.Pow(intY, 2)));
        }

        public static double AngleBetweenChars(PossibleChar firstChar, PossibleChar secondChar)
        {
            double dblAdj = Math.Abs(firstChar.IntCenterX - secondChar.IntCenterX);
            double dblOpp = Math.Abs(firstChar.IntCenterY - secondChar.IntCenterY);

            double dblAngleInRad = Math.Atan(dblOpp / dblAdj);

            double dblAngleInDeg = dblAngleInRad * (180.0 / Cv2.PI);

            return dblAngleInDeg;
        }

        public static ICollection<PossibleChar> FindPossibleCharsInScene(Mat preprocessedImage)
        {
            var imageContours = new Mat(preprocessedImage.Size(), MatType.CV_8UC3, Program.ScalarBlack);
            var preprocessedImageCopy = preprocessedImage.Clone();

            Cv2.FindContours(
                image: preprocessedImageCopy,
                contours: out Point[][] foundedContours,
                hierarchy: out HierarchyIndex[] hierarchy,
                mode: RetrievalModes.List,
                method: ContourApproximationModes.ApproxSimple);

            var listOfPossibleChars = new List<PossibleChar>();

            //ThreadPool.GetAvailableThreads(out var threadsCount, out var completionPortThreads);
            var threadsCount = 8;
            var contoursCountPerThread = foundedContours.Length / threadsCount;
            var rest = foundedContours.Length % threadsCount;
            var tasks = new List<Task<List<PossibleChar>>>();

            for (int thread = 0; thread < threadsCount; thread++)
            {
                var toSkip = contoursCountPerThread * thread;
                var count = contoursCountPerThread;
                if (thread + 1 >= threadsCount)
                    count = count + rest;
                var withoudSkipped = foundedContours.Skip(toSkip);
                var toCalculate = withoudSkipped.Take(contoursCountPerThread);
                tasks.Add(Task.Run(() => GetPossibleCharsFromContours(toCalculate, ref imageContours)));
            }

            Task.WaitAll(tasks.ToArray());
            foreach (var possibleChars in tasks.Select(x => x.Result))
            {
                listOfPossibleChars.AddRange(possibleChars);
            }

            imageContours = new Mat(preprocessedImageCopy.Size(), MatType.CV_8UC3, Program.ScalarBlack);

            var test = listOfPossibleChars.Select(x => x.Contour).ToArray();
            Cv2.DrawContours(imageContours, test, -1, Program.ScalarWhite);
            //Cv2.ImShow("Contours 1B", imageContours);

            return listOfPossibleChars;
        }

        private static List<PossibleChar> GetPossibleCharsFromContours(IEnumerable<Point[]> contours, ref Mat imageContours)
        {
            var result = new List<PossibleChar>();
            for (int i = 0; i < contours.Count(); ++i)
            {
                Cv2.DrawContours(imageContours, contours, i, Program.ScalarWhite);
                var possibleChar = new PossibleChar(contours.ElementAt(i));

                if (CheckIfPossibleChar(possibleChar))
                {
                    result.Add(possibleChar);
                }
            }
            return result;
        }
    }
}
