using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PolygonFilling.Structures;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Polygon = PolygonFilling.Structures.Polygon;
using Rectangle = System.Windows.Shapes.Rectangle;
using Vector = PolygonFilling.Structures.Vector;

namespace PolygonFilling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    ///
    /// 
    /// </summary>
    public partial class MainWindow
    {
        private Polygon _fillPolygon = new Polygon();
        private Polygon _clippPolygon = new Polygon();

        private readonly Brush _defaultPolygonColor = Brushes.Black;
        private Brush _fillColor = Brushes.Red;
        private Brush _lightColor = Brushes.White;

        //boole
        private bool _isColorInsteadOfTexturePolygonFill = true;
        private bool _isDefaultInsteadOfTextureNormalVector = true;
        private bool _isDefaultInsteadOfTextureHeightMap = true;
        private bool _isDefaultInsteadOfFixedLightVector = true;

        //wektory
        private Vector _lightVector = new Vector(0,0,1);
        private Vector _normalVector = new Vector(0,0,1);
        private Vector _disturbVector = new Vector(0,0,0);

        //bitmapy
        private string _defaultFillTextureFileName = "zlota_tekstura.jpg";
        private BitmapImage _fillPolygonTextureBitmapImage;
        private Bitmap _fillPolygonTexture;

        private string _defaultNormalVectorFileName = "normal_map.jpg";
        private BitmapImage _normalVectorBitmapImage;
        private Bitmap _normalVectorTexture;

        private string _defaultHeightMapFileName = "brick_heightmap.png";
        private BitmapImage _heightMapBitmapImage;
        private Bitmap _heightMapTexture;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTwoPolygons();
            InitializeDefaultTexture();
            EnableMovingVertexes();
        }

        private void InitializeTwoPolygons()
        {
            List<Point> fillPolygonPoints = new List<Point>();
            fillPolygonPoints.Add(new Point(50,150));
            fillPolygonPoints.Add(new Point(100, 100));
            fillPolygonPoints.Add(new Point(200, 100));
            fillPolygonPoints.Add(new Point(250, 150));
            fillPolygonPoints.Add(new Point(150, 200));

            _fillPolygon = CreateAndDrawNewPolygon(fillPolygonPoints);
            ColorPolygon();

            List<Point> clippPolygonPoints = new List<Point>();
            clippPolygonPoints.Add(new Point(300, 300));
            clippPolygonPoints.Add(new Point(500, 300));
            clippPolygonPoints.Add(new Point(700, 500));
            clippPolygonPoints.Add(new Point(600, 600));
            clippPolygonPoints.Add(new Point(350, 700));
            clippPolygonPoints.Add(new Point(300, 500));

            _clippPolygon = CreateAndDrawNewPolygon(clippPolygonPoints);

        }

        private Polygon CreateAndDrawNewPolygon(List<Point> pointsCoordinates)
        {
            Polygon newPolygon = new Polygon();

            foreach (var coordinates in pointsCoordinates)
            {
                newPolygon.AddNewVertex(coordinates, new Rectangle());
            }
            for (int i = 0; i < newPolygon.Vertexes.Count - 1; i++)
            {
                Vertex vertexOne = newPolygon.GetVertexByIndex(i);
                Vertex vertexTwo = newPolygon.GetVertexByIndex(i + 1);
                newPolygon.AddNewEdge(vertexOne, vertexTwo, CreateEdgeLine(vertexOne, vertexTwo));
            }
            Vertex vertexLast = newPolygon.GetVertexByIndex(newPolygon.Vertexes.Count - 1);
            Vertex vertexFirst = newPolygon.GetVertexByIndex(0);
            newPolygon.AddNewEdge(vertexLast, vertexFirst, CreateEdgeLine(vertexLast, vertexFirst));
            newPolygon.InitializeEdgeTable();

            return newPolygon;
        }

        private void RedrawVertexes()
        {
            DisableMovingVertexes();

            foreach (var vertex in _fillPolygon.Vertexes)
            {
                Canvas.Children.Remove(vertex.Pixel);
                vertex.Pixel = new Rectangle();
                vertex.SetNewPixel(vertex.X, vertex.Y, SetPixel(vertex.X, vertex.Y, _defaultPolygonColor));
            }

            EnableMovingVertexes();
        }

        private void InitializeDefaultTexture()
        {
            BitmapImage bmp = ConvertFileToBitmapImage(_defaultFillTextureFileName, false);
            FillPolygonTextureImage.Source = bmp;
            FillPolygonTextureImage.Height = 50;
            FillPolygonTextureImage.Width = 50;
            _fillPolygonTextureBitmapImage = bmp;

            BitmapImage bmp2 = ConvertFileToBitmapImage(_defaultNormalVectorFileName, false);
            NormalVectorTextureImage.Source = bmp2;
            NormalVectorTextureImage.Height = 50;
            NormalVectorTextureImage.Width = 50;
            _normalVectorBitmapImage = bmp2;

            BitmapImage bmp3 = ConvertFileToBitmapImage(_defaultHeightMapFileName, false);
            HeightMapTextureImage.Source = bmp3;
            HeightMapTextureImage.Height = 50;
            HeightMapTextureImage.Width = 50;
            _heightMapBitmapImage = bmp3;
        }

        public Bitmap ConvertImageToBitmap(BitmapImage bitmapImage, int height, int width)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap, width, height);
            }
        }

        public BitmapImage ConvertFileToBitmapImage(string fileNameOrPath, bool isFullPath)
        {
            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = isFullPath ? new Uri(fileNameOrPath) : new Uri(Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileNameOrPath));
                bmp.EndInit();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            return bmp;
        }

        private Point GetMousePosition(object mouse)
        {
            Point p = Mouse.GetPosition((Canvas)mouse);
            int x = (int)p.X;
            int y = (int)p.Y;
            return new Point(x,y);
        }

        private Rectangle SetPixel(int x, int y, Brush color, int size = 10)
        {
            Rectangle rectangle = new Rectangle() { Width = size, Height = size, Fill = color };
            Canvas.Children.Add(rectangle);
            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);
            return rectangle;
        }      

        private int _currentY = 0;

        private void ColorPolygonClick(object sender, RoutedEventArgs e)
        {
            ColorPolygon();
        }

        private void ColorPolygon()
        {
            if (_fillPolygon.Vertexes.Count < 3) return;

            InitializeTexturesBeforeColoring();
            _fillPolygon.PixelFill.Clear();

            List<EdgeTableElem> activeEdgeTable = new List<EdgeTableElem>();

            for (int y = _fillPolygon.YMin; y < _fillPolygon.YMax + 1; y += 4)
            {
                _currentY = y;
                if (_fillPolygon.EdgeTable[y].Count != 0)
                {
                    activeEdgeTable = _fillPolygon.EdgeTable[y].OrderBy(el => el.X).ToList();
                }
                _fillPolygon.PixelFill.Add(FillScanLineWithColor(activeEdgeTable));
            }
            RedrawVertexes();
        }

        private void InitializeTexturesBeforeColoring()
        {
            if (!_isColorInsteadOfTexturePolygonFill)
            {
                _fillPolygonTexture = ConvertImageToBitmap(_fillPolygonTextureBitmapImage, (int)Canvas.ActualHeight, (int)Canvas.ActualWidth);
            }

            if (!_isDefaultInsteadOfTextureNormalVector)
            {
                _normalVectorTexture = ConvertImageToBitmap(_normalVectorBitmapImage,
                    _fillPolygon.YMax - _fillPolygon.YMin, _fillPolygon.XMax - _fillPolygon.XMin);
            }

            if (!_isDefaultInsteadOfTextureHeightMap)
            {
                _heightMapTexture = ConvertImageToBitmap(_heightMapBitmapImage,
                    _fillPolygon.YMax - _fillPolygon.YMin, _fillPolygon.XMax - _fillPolygon.XMin);
            }

            if(!_isDefaultInsteadOfFixedLightVector)
            {
                _lightVector = new Vector(LightVectorXSlider.Value, LightVectorYSlider.Value, LightVectorZSlider.Value).Normalize();
            }
            else
            {
                _lightVector = new Vector(0,0,1);
            }
        }


        private List<Rectangle> FillScanLineWithColor(List<EdgeTableElem> activeEdgeTable)
        {
            var retValue = new List<Rectangle>();

            for (int i = 0; i < activeEdgeTable.Count - 1; i += 2)
            {
                retValue.AddRange(ColorPartOfScanLine(activeEdgeTable[i].Table[_currentY], activeEdgeTable[i + 1].Table[_currentY]));
            }

            return retValue;
        }

        private List<Rectangle> ColorPartOfScanLine(int xLeft, int xRight)
        {
            var retValue = new List<Rectangle>();
            int x = xLeft;

            while (x <= xRight)
            {
                if (_isColorInsteadOfTexturePolygonFill)
                {
                    if (!_isDefaultInsteadOfTextureNormalVector) SetNewNormalVector(x, _currentY);

                    if(!_isDefaultInsteadOfTextureHeightMap) SetNewDisturbVector(x, _currentY);

                    var color = LambertFormula();
                    retValue.Add(SetPixel(x, _currentY, color, 5));
                }
                else
                {
                    retValue.Add(SetPixel(x, _currentY, GetTexturePixel(x, _currentY) , 5));
                }
                
                x+= 4;
            }
            return retValue;
        }

        private void SetNewNormalVector(int x, int y)
        {
            System.Drawing.Color newColor = _normalVectorTexture.GetPixel(
                Math.Min(Math.Max(x - _fillPolygon.XMin, 1), _normalVectorTexture.Width - 1) ,
                Math.Min(Math.Max(y - _fillPolygon.YMin, 1), _normalVectorTexture.Height - 1) );
            _normalVector = new Vector(newColor);
            _normalVector = new Vector(_normalVector.X, _normalVector.Y, 1);
            _normalVector = _normalVector.Normalize();
        }

        private void SetNewDisturbVector(int x, int y)
        {
            System.Drawing.Color height = _heightMapTexture.GetPixel(
                Math.Min(Math.Max(x - _fillPolygon.XMin, 1), _heightMapTexture.Width - 1),
                Math.Min(Math.Max(y - _fillPolygon.YMin, 1), _heightMapTexture.Height - 1));

            Vector heightVector = new Vector(height);

            Vector t = new Vector(1, 0, -heightVector.X);
            Vector b = new Vector(0, 1, -heightVector.Y);

            System.Drawing.Color heightAddX = _heightMapTexture.GetPixel(
                Math.Min(Math.Max(x + 1 - _fillPolygon.XMin, 1), _heightMapTexture.Width - 1),
                Math.Min(Math.Max(y - _fillPolygon.YMin, 1), _heightMapTexture.Height - 1));

            System.Drawing.Color heightAddY = _heightMapTexture.GetPixel(
                Math.Min(Math.Max(x - _fillPolygon.XMin, 1), _heightMapTexture.Width - 1),
                Math.Min(Math.Max(y + 1 - _fillPolygon.YMin, 1), _heightMapTexture.Height - 1));

            double dhx = heightAddX.B - height.B;
            double dhy = heightAddY.B - height.B;

            Vector tMulDhx = t.MultiplyByNumber(dhx);
            Vector bMulDhy = b.MultiplyByNumber(dhy);

            _disturbVector = tMulDhx.AddVectors(bMulDhy);
            _disturbVector = _disturbVector.Normalize();
        }

        private Brush GetTexturePixel(int x, int y)
        {
            System.Drawing.Color color = _fillPolygonTexture.GetPixel(x, y);
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        private List<LinePixel> CreateEdgeLine(Vertex v1, Vertex v2, int size = 4)
        {
            int x1 = v1.X;
            int x2 = v2.X;
            int y1 = v1.Y;
            int y2 = v2.Y;

            List<LinePixel> listOfRectangles = new List<LinePixel>();

            int d, dx, dy, ai, bi, xi, yi;
            int x = x1, y = y1;
            // ustalenie kierunku rysowania
            if (x1 < x2)
            {
                xi = 1;
                dx = x2 - x1;
            }
            else
            {
                xi = -1;
                dx = x1 - x2;
            }
            // ustalenie kierunku rysowania
            if (y1 < y2)
            {
                yi = 1;
                dy = y2 - y1;
            }
            else
            {
                yi = -1;
                dy = y1 - y2;
            }

            // pierwszy piksel
            var rectangle = SetPixel(x, y, _defaultPolygonColor, size);
            listOfRectangles.Add(new LinePixel(x,y,rectangle));            

            // oś wiodąca OX
            if (dx > dy)
            {
                ai = (dy - dx) * 2;
                bi = dy * 2;
                d = bi - dx;
                // pętla po kolejnych x
                while (x != x2)
                {
                    // test współczynnika
                    if (d >= 0)
                    {
                        x += xi;
                        y += yi;
                        d += ai;
                    }
                    else
                    {
                        d += bi;
                        x += xi;
                    }
                    rectangle = SetPixel(x, y, _defaultPolygonColor, size);
                    listOfRectangles.Add(new LinePixel(x, y, rectangle));
                }
            }
            // oś wiodąca OY
            else
            {
                ai = (dx - dy) * 2;
                bi = dx * 2;
                d = bi - dy;
                // pętla po kolejnych y
                while (y != y2)
                {
                    // test współczynnika
                    if (d >= 0)
                    {
                        x += xi;
                        y += yi;
                        d += ai;
                    }
                    else
                    {
                        d += bi;
                        y += yi;
                    }
                    rectangle = SetPixel(x, y, _defaultPolygonColor, size);
                    listOfRectangles.Add(new LinePixel(x, y, rectangle));
                }
            }
            return listOfRectangles;
        }

        private void PolygonFillColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (PolygonFillColorPicker.SelectedColor != null)
            {
                _fillColor = new SolidColorBrush((Color)PolygonFillColorPicker.SelectedColor);
            }            
        }      

        #region Clipping

        private Point GetIntersectionCoordinates(Edge e1, Edge e2)
        {
            int x1 = e1.VertexOne.X;
            int y1 = e1.VertexOne.Y;

            int x2 = e1.VertexTwo.X;
            int y2 = e1.VertexTwo.Y;

            int x3 = e2.VertexOne.X;
            int y3 = e2.VertexOne.Y;

            int x4 = e2.VertexTwo.X;
            int y4 = e2.VertexTwo.Y;

            int x = (((x1 * y2 - y1 * x2) * (x3 - x4)) - ((x1 - x2) * (x3 * y4 - y3 * x4))) /
                    (((x1 - x2) * (y3 - y4)) - ((y1 - y2) * (x3 - x4)));

            int y = (((x1 * y2 - y1 * x2) * (y3 - y4)) - ((y1 - y2) * (x3 * y4 - y3 * x4))) /
                    (((x1 - x2) * (y3 - y4)) - ((y1 - y2) * (x3 - x4)));

            return new Point(x, y);
        }       

        private int Det(Vertex v1, Vertex v2)
        {
            return ((v1.X * v2.Y) - (v1.Y * v2.X));
        }

        private double Det(Point p1, Point p2)
        {
            return ((p1.X * p2.Y) - (p1.Y * p2.X));
        }

        private bool CheckIfCanIntersect(Edge e1, Edge e2)
        {
            Point p = new Point(e1.VertexOne.X, e1.VertexOne.Y);
            Point r = new Point(e1.VertexTwo.X - p.X, e1.VertexTwo.Y - p.Y);
            Point q = new Point(e2.VertexOne.X, e2.VertexOne.Y);
            Point s = new Point(e2.VertexTwo.X - q.X, e2.VertexTwo.Y - q.Y);

            double t = Det(new Point(q.X - p.X, q.Y - p.Y), s) / Det(r, s);
            double u = Det(new Point(q.X - p.X, q.Y - p.Y), r) / Det(r, s);

            return (u > 0 && u < 1) && (t > 0 && t < 1);
        }

        private void GenerateListsWithIntersectionPoints(Polygon polygonOne, Polygon polygonTwo, out List<Vertex> vertexesOne, out List<Vertex> vertexesTwo, out Vertex startVertex)
        {
            vertexesOne = new List<Vertex>(polygonOne.Vertexes);
            vertexesTwo = new List<Vertex>(polygonTwo.Vertexes);

            List<Edge> edgesOne = new List<Edge>(polygonOne.Edges);
            List<Edge> edgesTwo = new List<Edge>(polygonTwo.Edges);
            startVertex = null;
            bool isStartVertexSet = false;
            bool breakForeachLoop = false;

            while (true)
            {
                foreach (var edgeOne in edgesOne)
                {
                    foreach (var edgeTwo in edgesTwo)
                    {
                        if (CheckIfCanIntersect(edgeOne, edgeTwo) && !(edgeOne.WasIntersected && edgeTwo.WasIntersected))
                        {
                            if (!isStartVertexSet)
                            {
                                startVertex = CheckIfCanBeStartVertex(edgeOne.VertexOne, edgeOne.VertexTwo, edgeTwo,
                                    out isStartVertexSet);
                            }

                            Point coordinates = GetIntersectionCoordinates(edgeOne, edgeTwo);

                            Vertex vOne = new Vertex(vertexesOne.Count, coordinates, new Rectangle());
                            vOne.IsIntersected = true;
                            int indexOne = vertexesOne.IndexOf(edgeOne.VertexTwo);
                            vertexesOne.Insert(indexOne, vOne);

                            Vertex vTwo = new Vertex(vertexesTwo.Count, coordinates, new Rectangle());
                            vTwo.IsIntersected = true;
                            int indexTwo = vertexesTwo.IndexOf(edgeTwo.VertexTwo);
                            vertexesTwo.Insert(indexTwo, vTwo);

                            breakForeachLoop = true;
                            break;
                        }
                    }
                    if (breakForeachLoop) break;
                }

                ReorganizeEdgesAndVertexes(vertexesOne, vertexesTwo, edgesOne, edgesTwo);  // nie jestem pewien czy zmieni te wartości

                if (breakForeachLoop)
                {
                    breakForeachLoop = false;
                }
                else
                {
                    break;
                }
            }            
        }

        private void ReorganizeEdgesAndVertexes(List<Vertex> vertexesOne, List<Vertex> vertexesTwo, List<Edge> edgesOne, List<Edge> edgesTwo)
        {
            edgesOne.Clear();
            edgesTwo.Clear();

            Edge newEdgeToAdd = new Edge();

            for (int i = 0; i < vertexesOne.Count - 1; i++)
            {
                Vertex vertexOne = vertexesOne[i];
                Vertex vertexTwo = vertexesOne[i + 1];
                newEdgeToAdd = new Edge(vertexOne, vertexTwo, new List<LinePixel>());
                if (vertexOne.IsIntersected || vertexTwo.IsIntersected) newEdgeToAdd.WasIntersected = true;
                edgesOne.Add(newEdgeToAdd);
            }
            Vertex vertexLast = vertexesOne[vertexesOne.Count - 1];
            Vertex vertexFirst = vertexesOne[0];
            newEdgeToAdd = new Edge(vertexLast, vertexFirst, new List<LinePixel>());
            if (vertexLast.IsIntersected || vertexFirst.IsIntersected) newEdgeToAdd.WasIntersected = true;
            edgesOne.Add(newEdgeToAdd);

            for (int i = 0; i < vertexesTwo.Count - 1; i++)
            {
                Vertex vertexOne = vertexesTwo[i];
                Vertex vertexTwo = vertexesTwo[i + 1];
                newEdgeToAdd = new Edge(vertexOne, vertexTwo, new List<LinePixel>());
                if (vertexOne.IsIntersected || vertexTwo.IsIntersected) newEdgeToAdd.WasIntersected = true;
                edgesTwo.Add(newEdgeToAdd);
            }
            vertexLast = vertexesTwo[vertexesTwo.Count - 1];
            vertexFirst = vertexesTwo[0];
            newEdgeToAdd = new Edge(vertexLast, vertexFirst, new List<LinePixel>());
            if (vertexLast.IsIntersected || vertexFirst.IsIntersected) newEdgeToAdd.WasIntersected = true;
            edgesTwo.Add(newEdgeToAdd);
        }

        private Vertex CheckIfCanBeStartVertex(Vertex vertexOne, Vertex vertexTwo, Edge edge, out bool isStartVertexSet)
        {

            Point p0 = new Point(edge.VertexOne.X, edge.VertexOne.Y);
            Point p1 = new Point(edge.VertexTwo.X, edge.VertexTwo.Y);
            Point p2 = new Point(vertexOne.X, vertexOne.Y);
            Point p3 = new Point(vertexTwo.X, vertexTwo.Y);

            double a = ((p2.X - p0.X) * (p1.Y - p0.Y)) - ((p2.Y - p0.Y) * (p1.X - p0.X));
            double b = ((p3.X - p0.X) * (p1.Y - p0.Y)) - ((p3.Y - p0.Y) * (p1.X - p0.X));

            if (a > 0 && b < 0)
            {
                isStartVertexSet = true;
                return vertexTwo;
            }

            isStartVertexSet = false;
            return null;
        }

        private void ClipPolygons(object sender, RoutedEventArgs e)
        {
            if (_fillPolygon.Vertexes.Count < 3 || _clippPolygon.Vertexes.Count < 3) return;

            List<Vertex> clipPolygonOneVertexes = new List<Vertex>();
            List<Vertex> clipPolygonTwoVertexes = new List<Vertex>();
            Vertex startVertex = null;

            GenerateListsWithIntersectionPoints(_fillPolygon, _clippPolygon, out clipPolygonOneVertexes, out clipPolygonTwoVertexes, out startVertex);

            if (clipPolygonOneVertexes.Count != _fillPolygon.Vertexes.Count)
            {
                if(startVertex == null) return;

                ErasePolygonFromCanvas(_fillPolygon);
                ErasePolygonFromCanvas(_clippPolygon);

                List<Point> newPolygonPointsCoordinates = CreateClippedPolygon(clipPolygonOneVertexes, clipPolygonTwoVertexes, startVertex);
                _fillPolygon = CreateAndDrawNewPolygon(newPolygonPointsCoordinates);
                ColorPolygon();
            }
        }

        private List<Point> CreateClippedPolygon(List<Vertex> clipPolygonOneVertexes, List<Vertex> clipPolygonTwoVertexes,
            Vertex startVertex)
        {

            List<Vertex> currentVertexList = clipPolygonOneVertexes;
            bool isCurrentListOne = true;
            Vertex currentVertex = startVertex;
            int currentIndex = currentVertexList.IndexOf(startVertex);
            List<Vertex> newPolygonVertexes = new List<Vertex>();
            int nextIndex = 0;

            while (true)
            {
                currentVertex.IsVisited = true;
                newPolygonVertexes.Add(currentVertex);
                nextIndex = currentIndex == currentVertexList.Count - 1 ? 0 : currentIndex + 1;

                if (currentVertexList[nextIndex].X == startVertex.X && currentVertexList[nextIndex].Y == startVertex.Y) break;

                currentVertex = currentVertexList[nextIndex];
                currentIndex = nextIndex;
                if (currentVertex.IsIntersected)
                {
                    if (isCurrentListOne)
                    {
                        isCurrentListOne = false;
                        currentVertexList = clipPolygonTwoVertexes;
                    }
                    else
                    {
                        isCurrentListOne = true;
                        currentVertexList = clipPolygonOneVertexes;
                    }
                    currentIndex =
                        currentVertexList.IndexOf(
                            currentVertexList.FirstOrDefault(v => v.X == currentVertex.X && v.Y == currentVertex.Y));
                }

            }

            List<Point> retValueList = new List<Point>();
            foreach (var vertex in newPolygonVertexes)
            {
                retValueList.Add(new Point(vertex.X, vertex.Y));
            }

            return retValueList;
        }



        #endregion

        private void ErasePolygonFromCanvas(Polygon polygon)
        {
            foreach (var vertex in polygon.Vertexes)
            {
                Canvas.Children.Remove(vertex.Pixel);
            }
            foreach (var edge in polygon.Edges)
            {
                foreach (var linePixel in edge.Line)
                {
                    Canvas.Children.Remove(linePixel.Rectangle);
                }
            }
            foreach (var list in polygon.PixelFill)
            {
                foreach (var rectangle in list)
                {
                    Canvas.Children.Remove(rectangle);
                }
            }
        }

        private Brush LambertFormula()
        {
            int rgbCount = 255;

            Vector objectColorVector = new Vector(_fillColor);
            Vector lightColorVector = new Vector(_lightColor);

            Vector normalAddedWithDisurbVector = _normalVector.AddVectors(_disturbVector);
            Vector normalDisurbVector = normalAddedWithDisurbVector.Normalize();

            double cos = normalDisurbVector.DotProduct(_lightVector);

            int r = (int)((lightColorVector.X * objectColorVector.X * cos) * rgbCount);
            int g = (int)((lightColorVector.Y * objectColorVector.Y * cos) * rgbCount);
            int b = (int)((lightColorVector.Z * objectColorVector.Z * cos) * rgbCount);

            Color color = new Color
            {
                R = (byte)r,
                G = (byte)g,
                B = (byte)b,
                A = 255
            };

            return new SolidColorBrush(color);
        }

        #region GUI

        private void LightColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (LightColorPicker.SelectedColor != null)
            {
                _lightColor = new SolidColorBrush((Color)LightColorPicker.SelectedColor);
            }
        }

        public void LoadFillPolygonTexture(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|BMP (*.bmp)|*.bmp",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;

                BitmapImage bmp = ConvertFileToBitmapImage(path, true);
                FillPolygonTextureImage.Source = bmp;
                FillPolygonTextureImage.Height = 50;
                FillPolygonTextureImage.Width = 50;

                _fillPolygonTextureBitmapImage = bmp;
            }
        }

        public void LoadNormalVectorTexture(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|BMP (*.bmp)|*.bmp",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;

                BitmapImage bmp2 = ConvertFileToBitmapImage(path, true);
                NormalVectorTextureImage.Source = bmp2;
                NormalVectorTextureImage.Height = 50;
                NormalVectorTextureImage.Width = 50;
                _normalVectorBitmapImage = bmp2;

                _normalVectorBitmapImage = bmp2;
            }
        }

        public void LoadHeightMapTexture(object sender, RoutedEventArgs e) // napraw
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|BMP (*.bmp)|*.bmp",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;

                BitmapImage bmp3 = ConvertFileToBitmapImage(path, true);
                HeightMapTextureImage.Source = bmp3;
                HeightMapTextureImage.Height = 50;
                HeightMapTextureImage.Width = 50;

                _heightMapBitmapImage = bmp3;
            }
        }

        private void ChooseColorFillPolygon(object sender, RoutedEventArgs e)
        {
            _isColorInsteadOfTexturePolygonFill = true;
        }

        private void ChooseTextureFillPolygon(object sender, RoutedEventArgs e)
        {
            _isColorInsteadOfTexturePolygonFill = false;
        }

        private void ChooseDefaultNormalVector(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfTextureNormalVector = true;
            _normalVector = new Vector(0,0,1);
        }

        private void ChooseTextureNormalVector(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfTextureNormalVector = false;
        }

        private void ChooseDefaultHeightMap(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfTextureHeightMap = true;
            _disturbVector = new Vector(0,0,0);
        }

        private void ChooseTextureHeightMap(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfTextureHeightMap = false;
        }

        private void ChooseDefaultLightVector(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfFixedLightVector = true;
        }

        private void ChooseFixedLightVector(object sender, RoutedEventArgs e)
        {
            _isDefaultInsteadOfFixedLightVector = false;
        }

        #endregion

        #region Moving

        private void EnableMovingPolygon()
        {
            Canvas.MouseLeftButtonDown += LeftButtonDownPolygon;
            Canvas.MouseLeftButtonUp += LeftButtonUpPolygon;
        }

        private void DisableMovingPolygon()
        {
            Canvas.MouseLeftButtonDown -= LeftButtonDownPolygon;
            Canvas.MouseLeftButtonUp -= LeftButtonUpPolygon;
        }

        private Vertex _movingVertex;

        private void EnableMovingVertexes()
        {
            foreach (var ver in _fillPolygon.Vertexes)
            {
                ver.Pixel.MouseLeftButtonDown += LeftButtonDownVertex;
            }
            Canvas.MouseLeftButtonUp += LeftButtonUpVertex;
        }

        private void DisableMovingVertexes()
        {
            foreach (var ver in _fillPolygon.Vertexes)
            {
                ver.Pixel.MouseLeftButtonDown -= LeftButtonDownVertex;
            }
            Canvas.MouseLeftButtonUp -= LeftButtonUpVertex;
        }

        private void LeftButtonDownVertex(object sender, MouseButtonEventArgs e)
        {
            Rectangle rectangle = sender as Rectangle;
            if (rectangle != null)
            {
                _movingVertex = FindVerticeByRectangle(rectangle);
            }
        }

        private void LeftButtonUpVertex(object sender, MouseButtonEventArgs e)
        {
            //ustalenie pozycji myszki
            Point coords = GetMousePosition(sender);
            int x = (int) coords.X;
            int y = (int)coords.Y;

            MoveVerticle(x,y);

            _movingVertex = null;
        }

        private void MoveVerticle(int x, int y)
        {
            if(_movingVertex == null) return;

            int vertexId = _movingVertex.Id;
            List<Edge> edges = new List<Edge>(_fillPolygon.Edges.Where(l => l.VertexOne.Id == vertexId || l.VertexTwo.Id == vertexId));

            Canvas.Children.Remove(_movingVertex.Pixel);
            foreach (var edge in edges)
            {
                ClearEdge(edge);
                edge.Line.Clear();
            }

            // -------------                

            // nowy punkt wierzchołka
            Rectangle pixel = SetPixel(x, y, _defaultPolygonColor);

            pixel.MouseLeftButtonDown += LeftButtonDownVertex;
            pixel.MouseLeftButtonUp += LeftButtonUpVertex;
            _movingVertex.SetNewPixel(x, y, pixel);

            // --------------------

            // przerysowanie linii

            foreach (var edge in edges)
            {
                if (edge.VertexOne.Id == vertexId)
                {
                    edge.VertexOne = _movingVertex;
                }
                else
                {
                    edge.VertexTwo = _movingVertex;
                }
                edge.Line = CreateEdgeLine(edge.VertexOne, edge.VertexTwo);
            }

            //--------------------------------   

            // przerysowanie wnetrza

            foreach (var list in _fillPolygon.PixelFill)
            {
                foreach (var rectangle in list)
                {
                    Canvas.Children.Remove(rectangle);
                }
            }
            _fillPolygon.EdgeTable = new List<EdgeTableElem>[0];
            _fillPolygon.InitializeEdgeTable();
            ColorPolygon();

            //-----------------------
        }

        private void ClearEdge(Edge edge)
        {
            foreach (var linePixel in edge.Line)
            {
                Canvas.Children.Remove(linePixel.Rectangle);
            }
        }

        private Vertex FindVerticeByRectangle(Rectangle rectangle)
        {
            return _fillPolygon.Vertexes.FirstOrDefault(v => Equals(v.Pixel, rectangle));
        }

        private void AllowMovingPolygon(object sender, RoutedEventArgs e)
        {
            DisableMovingVertexes();
            EnableMovingPolygon();

            foreach (var vertex in _fillPolygon.Vertexes)
            {
                vertex.Pixel.MouseLeftButtonUp += LeftButtonUpPolygon;
            }
        }

        private void ForbidMovingPolygon(object sender, RoutedEventArgs e)
        {
            EnableMovingVertexes();
            DisableMovingPolygon();

            foreach (var vertex in _fillPolygon.Vertexes)
            {
                vertex.Pixel.MouseLeftButtonUp -= LeftButtonUpPolygon;
            }
        }
    

        private Point _previousMousePosition = new Point();

        private void LeftButtonDownPolygon(object sender, MouseButtonEventArgs e)
        {
            _previousMousePosition = GetMousePosition(sender);
        }

        private void LeftButtonUpPolygon(object sender, MouseButtonEventArgs e)
        {
            Point _currentMousePosition = GetMousePosition(sender);

            int dx = (int)(_previousMousePosition.X - _currentMousePosition.X);
            int dy = (int)(_previousMousePosition.Y - _currentMousePosition.Y);

            MovePolygon(dx, dy);

        }

        private void MovePolygon(int dx, int dy)
        {            
            ErasePolygonFromCanvas(_fillPolygon);
            _fillPolygon.Edges.Clear();

            foreach (var vertex in _fillPolygon.Vertexes)
            {
                vertex.X -= dx;
                vertex.Y -= dy;
                vertex.SetNewPixel(vertex.X, vertex.Y, SetPixel(vertex.X, vertex.Y, _defaultPolygonColor));
            }

            for (int i = 0; i < _fillPolygon.Vertexes.Count - 1; i++)
            {
                Vertex vertexOne = _fillPolygon.GetVertexByIndex(i);
                Vertex vertexTwo = _fillPolygon.GetVertexByIndex(i + 1);
                _fillPolygon.AddNewEdge(vertexOne, vertexTwo, CreateEdgeLine(vertexOne, vertexTwo));
            }
            Vertex vertexLast = _fillPolygon.GetVertexByIndex(_fillPolygon.Vertexes.Count - 1);
            Vertex vertexFirst = _fillPolygon.GetVertexByIndex(0);
            _fillPolygon.AddNewEdge(vertexLast, vertexFirst, CreateEdgeLine(vertexLast, vertexFirst));

            _fillPolygon.EdgeTable = new List<EdgeTableElem>[0];
            _fillPolygon.InitializeEdgeTable();
            ColorPolygon();
            
        }


        #endregion
        
    }
}
