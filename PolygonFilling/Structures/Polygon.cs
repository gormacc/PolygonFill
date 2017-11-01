using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class Polygon
    {
        public List<Vertex> Vertexes { get; set; } = new List<Vertex>();

        public List<Edge> Edges { get; set; } = new List<Edge>();

        private int vertexIndexer = 0;

        public Polygon()
        {
            
        }

        public Polygon(List<Vertex> vertexes, List<Edge> edges)
        {
            Vertexes = vertexes;
            Edges = edges;
        }

        public void AddNewEdge(Vertex vertexOne, Vertex vertexTwo, Line line)
        {
            Edges.Add(new Edge(vertexOne, vertexTwo, line));
        }

        public void AddNewVertex(Point coordinates, Rectangle pixel)
        {
            Vertexes.Add(new Vertex(vertexIndexer++, coordinates, pixel));
        }

        public Vertex GetVertexById(int id)
        {
            return Vertexes.FirstOrDefault(v => v.Id == id);
        }

        public Vertex GetVertexByPixel(Rectangle pixel)
        {
            return Vertexes.FirstOrDefault(v => Equals(v.Pixel, pixel));
        }

        public Vertex GetVertexByIndex(int index)
        {
            if (index < 0 || index >= Vertexes.Count)
            {
                return new Vertex();
            }
            else
            {
                return Vertexes[index];
            }
        }

        public Vertex GetLastVertex()
        {
            return Vertexes.LastOrDefault();
        }

        public Vertex GetFirstVertex()
        {
            return Vertexes.FirstOrDefault();
        }

    }
}
