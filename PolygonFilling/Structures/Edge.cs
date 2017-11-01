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
    }
}
