using System;
using System.Collections.Generic;

namespace PolygonFilling.Structures
{
    public class Edge
    {
        public Vertex VertexOne { get; set; } = new Vertex() {Id = -1};

        public Vertex VertexTwo { get; set; } = new Vertex() {Id = -1};

        public List<LinePixel> Line { get; set; } = new List<LinePixel>();

        public bool WasIntersected { get; set; } = false;

        public Edge()
        {

        }

        public Edge(Vertex vertexOne, Vertex vertexTwo, List<LinePixel> line)
        {
            VertexOne = vertexOne;
            VertexTwo = vertexTwo;
            Line = line;
        }

        public int LowerY => (int)Math.Min(VertexOne.Y, VertexTwo.Y);

        public int GreaterY => (int)Math.Max(VertexOne.Y, VertexTwo.Y);

        public int LowerX => (int)Math.Min(VertexOne.X, VertexTwo.X);

        public int GreaterX => (int)Math.Max(VertexOne.X, VertexTwo.X);
    }
}
