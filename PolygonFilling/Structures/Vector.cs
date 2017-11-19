using System;
using System.Windows.Media;

namespace PolygonFilling.Structures
{
    public class Vector
    {
        private const int RgbCount = 255;

        public double X { get; set; } = 0.0;
        public double Y { get; set; } = 0.0;
        public double Z { get; set; } = 0.0;

        public Vector()
        {
            
        }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector(Brush brush)
        {
            int r = (int)((Color) brush.GetValue(SolidColorBrush.ColorProperty)).R;
            int g = (int)((Color)brush.GetValue(SolidColorBrush.ColorProperty)).G;
            int b = (int)((Color)brush.GetValue(SolidColorBrush.ColorProperty)).B;

            X = (double)r / RgbCount;
            Y = (double)g / RgbCount;
            Z = (double)b / RgbCount;
        }

        public Vector(System.Drawing.Color color)
        {
            int r = color.R;
            int g = color.G;
            int b = color.B;

            X = (double)r / RgbCount;
            Y = (double)g / RgbCount;
            Z = (double)b / RgbCount;
        }

        public Vector Normalize()
        {
            double divisor = Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
            return new Vector(X/divisor , Y/divisor, Z / divisor);
        }

        public double DotProduct(Vector vec)
        {
            return (this.X * vec.X) + (this.Y * vec.Y) + (this.Z * vec.Z);
        }

        public Vector AddVectors(Vector vec)
        {
            return new Vector(this.X + vec.X, this.Y + vec.Y , this.Z + vec.Z);
        }

        public Vector MultiplyByNumber(double number)
        {
            return new Vector(this.X * number, this.Y * number, this.Z * number);
        }

        public Brush GetColorFromVector()
        {
            if (X > 1 || Y > 1 || Z > 1) return Brushes.White;

            int r = (int)(X * RgbCount);
            int g = (int)(Y * RgbCount);
            int b = (int)(Z * RgbCount);

            Color color = new Color
            {
                R = (byte) r,
                G = (byte) g,
                B = (byte) b
            };

            return new SolidColorBrush(color);
        }


    }
}
