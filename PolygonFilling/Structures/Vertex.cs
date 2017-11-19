using System.Windows;
using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class Vertex
    {
        public int X { get; set; } = 0;

        public int Y { get; set; } = 0;

        public int Id { get; set; }

        public Rectangle Pixel { get; set; } = new Rectangle();

        public bool IsIntersected { get; set; } = false;

        public bool IsVisited { get; set; } = false;

        public Vertex()
        {
            
        }

        public Vertex(int id, Point coordinates, Rectangle pixel)
        {
            Id = id;
            X = (int)coordinates.X;
            Y = (int)coordinates.Y;
            Pixel = pixel;
        }

        public void SetNewPixel(int x, int y, Rectangle pixel)
        {
            X = x;
            Y = y;
            Pixel = pixel;
        }

    }
}
