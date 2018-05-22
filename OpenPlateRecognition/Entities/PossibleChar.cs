using OpenCvSharp;
using System;
using System.Linq;

namespace LicensePlateRecognition
{
    public class PossibleChar
    {
        public Point[] Contour { get; private set; }
        public Rect BoundingRect { get; private set; }
        public double BoundingRectArea { get; private set; }
        public int IntCenterX { get; private set; }
        public int IntCenterY { get; private set; }
        public double DblDiagonalSize { get; private set; }
        public double DblAspectRatio { get; private set; }

        public PossibleChar(Point[] contour)
        {
            this.Contour = contour;
            this.BoundingRect = Cv2.BoundingRect(contour);
            this.BoundingRectArea = BoundingRect.Height * BoundingRect.Width;
            this.IntCenterX = (BoundingRect.X + BoundingRect.X + BoundingRect.Width) / 2;
            this.IntCenterY = (BoundingRect.Y + BoundingRect.Y + BoundingRect.Height) / 2;
            this.DblDiagonalSize = Math.Sqrt(Math.Pow(BoundingRect.Width, 2) + Math.Pow(BoundingRect.Height, 2));
            this.DblAspectRatio = (double)BoundingRect.Width / BoundingRect.Height;
        }

        public override bool Equals(object obj)
        {
            if(obj is PossibleChar possibleChar)
            {
                return Enumerable.SequenceEqual(this.Contour, possibleChar.Contour);
            }
            return false;
        }
    }
}
