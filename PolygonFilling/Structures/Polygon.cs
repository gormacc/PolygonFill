using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class Polygon
    {
        public List<Vertex> Vertexes { get; set; } = new List<Vertex>();

        public List<Edge> Edges { get; set; } = new List<Edge>();

        public List<EdgeTableElem>[] EdgeTable { get; set; } = new List<EdgeTableElem>[0];

        public List<List<Rectangle>> PixelFill { get; set; } = new List<List<Rectangle>>();

        public int YMin { get; set; } = 0;

        public int YMax { get; set; } = 0;

        private int vertexIndexer = 0;

        public Polygon()
        {
            
        }

        public Polygon(List<Vertex> vertexes, List<Edge> edges)
        {
            Vertexes = vertexes;
            Edges = edges;
        }

        public void AddNewEdge(Vertex vertexOne, Vertex vertexTwo, List<LinePixel> line)
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

        public void InitializeEdgeTable()
        {
            int maxY = int.MinValue;
            int minY = int.MaxValue;

            foreach (var edge in Edges)
            {
                if (edge.GreaterY > maxY)
                {
                    maxY = edge.GreaterY;
                }
                if (edge.LowerY < minY)
                {
                    minY = edge.LowerY;
                }
            }
            YMin = minY;
            YMax = maxY;
            EdgeTable = new List<EdgeTableElem>[maxY + 1];
            for (int i = 0; i < maxY + 1; i++)
            {
                EdgeTable[i] = new List<EdgeTableElem>();
            }
            foreach (var edge in Edges)
            {
                int[] table = new int[edge.GreaterY + 1];
                for (int i = 0; i < edge.GreaterY + 1; i++)
                {
                    table[i] = 0;
                }
                foreach (var linePixel in edge.Line)
                {
                    table[linePixel.Y] = linePixel.X;
                }
                EdgeTableElem entry = new EdgeTableElem(edge.GreaterY, Nacyhylenie(edge) == 0 ? edge.LowerX : edge.GreaterX , table);
                for (int i = edge.LowerY; i < edge.GreaterY; i++)
                {
                    EdgeTable[i].Add(entry);
                }
            }
        }

        private int Nacyhylenie(Edge edge)
        {
            if (edge.VertexOne.X == edge.VertexTwo.X)
            {
                return 0;
            }

            if((edge.VertexOne.X > edge.VertexTwo.X && edge.VertexOne.Y > edge.VertexTwo.Y ) || (edge.VertexOne.X < edge.VertexTwo.X && edge.VertexOne.Y < edge.VertexTwo.Y))
            {
                return 1;
            }

            return -1;
        }

    }
}
