using System;
using System.Windows.Shapes;

namespace PolygonFilling.Structures
{
    public class Edge
    {
        public Vertex VertexOne { get; set; } = new Vertex() {Id = -1};

        public Vertex VertexTwo { get; set; } = new Vertex() {Id = -1};

        public Line Lin { get; set; } = new Line();

        public Edge()
        {

        }

        public Edge(Vertex vertexOne, Vertex vertexTwo, Line line)
        {
            VertexOne = vertexOne;
            VertexTwo = vertexTwo;
            Lin = line;
        }

        public int LowerY => (int)Math.Min(VertexOne.Coordinates.Y, VertexTwo.Coordinates.Y);

        public int GreaterY => (int)Math.Max(VertexOne.Coordinates.Y, VertexTwo.Coordinates.Y);

        public int LowerX => (int)Math.Min(VertexOne.Coordinates.X, VertexTwo.Coordinates.X);

        public int GreaterX => (int)Math.Max(VertexOne.Coordinates.X, VertexTwo.Coordinates.X);
    }
}
