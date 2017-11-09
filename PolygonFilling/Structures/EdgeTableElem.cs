namespace PolygonFilling.Structures
{
    public class EdgeTableElem
    {
        public int Ymax { get; set; } = 0;

        public double X { get; set; } = 0.0;

        public double NextXVal { get; set; } = 0.0;

        public EdgeTableElem()
        {
            
        }

        public EdgeTableElem(int ymax, int x, double nextxval)
        {
            Ymax = ymax;
            X = x;
            NextXVal = nextxval;
        }
    }
}
