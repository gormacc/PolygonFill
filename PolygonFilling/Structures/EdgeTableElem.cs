namespace PolygonFilling.Structures
{
    public class EdgeTableElem
    {
        public int Ymax { get; set; } = 0;

        public int X { get; set; } = 0;

        public int[] Table { get; set; } = new int[0];

        public EdgeTableElem()
        {
            
        }

        public EdgeTableElem(int ymax, int x, int[] table)
        {
            Ymax = ymax;
            X = x;
            Table = table;
        }
    }
}
