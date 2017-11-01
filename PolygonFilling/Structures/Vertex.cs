using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class Vertex
    {
        public Point Coordinates { get; set; } = new Point();

        public int Id { get; set; }

        public Rectangle Pixel { get; set; } = new Rectangle();

        public Vertex()
        {
            
        }

        public Vertex(int id, Point coordinates, Rectangle pixel)
        {
            Id = id;
            Coordinates = coordinates;
            Pixel = pixel;
        }

    }
}
