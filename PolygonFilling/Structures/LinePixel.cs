using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class LinePixel
    {
        public int X = 0;
        public int Y = 0;
        public Rectangle Rectangle;

        public LinePixel()
        {
            
        }

        public LinePixel(int x, int y, Rectangle rectangle)
        {
            X = x;
            Y = y;
            Rectangle = rectangle;
        }
    }
}
