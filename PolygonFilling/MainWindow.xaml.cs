using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PolygonFilling.Structures;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
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
        private readonly List<Polygon> _polygons = new List<Polygon>();
        private Polygon _currentPolygon = new Polygon();
        private Polygon _selectedPolygon = new Polygon();
        private int _selectedPolygonIndex;

        private Polygon _selectedClippingPolygonOne = new Polygon();
        private int _selectedClippingPolygonOneIndex;
        private Polygon _selectedClippingPolygonTwo = new Polygon();
        private int _selectedClippingPolygonTwoIndex;
        private readonly Brush _defaultSelectedClippingPolygonOneColor = Brushes.Green;
        private readonly Brush _defaultSelectedClippingPolygonTwoColor = Brushes.Red;

        private readonly Brush _defaultPolygonColor = Brushes.Black;
        private readonly ContextMenu _verticleContextMenu = new ContextMenu();
        private Brush _fillColor = Brushes.Red;
        private Brush _lightColor = Brushes.White;
        private readonly Brush _defaultSelectedPolygonColor = Brushes.Green;

        //boole
        private bool _isColorInsteadOfTexture = true;

        //wektory
        private Vector _lightVersor = new Vector(0,0,1);
        private Vector _normalVector = new Vector(0,0,1);
        private Vector _disturbVector = new Vector(0,0,0);

        //bitmapy
        private string _defaultFillTextureFileName = "zlota_tekstura.jpg";
        private BitmapImage _fillPolygonTextureBitmapImage;
        private Bitmap _fillPolygonTexture;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVerticleContextMenu();
            InitializeDefaultTexture();
        }

        private void InitializeVerticleContextMenu()
        {
            MenuItem endDrawingPolygonMenuItem = new MenuItem { Header = "Zakoncz rysowanie wielokata" };
            endDrawingPolygonMenuItem.Click += EndPolygon;

            _verticleContextMenu.Items.Add(endDrawingPolygonMenuItem);
        }

        private void InitializeDefaultTexture()
        {
            BitmapImage bmp = ConvertFileToBitmapImage(_defaultFillTextureFileName, false);
            FillPolygonTextureImage.Source = bmp;
            FillPolygonTextureImage.Height = 50;
            FillPolygonTextureImage.Width = 50;

            _fillPolygonTextureBitmapImage = bmp;           
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

        private void SetVerticle(object sender, MouseButtonEventArgs e)
        {
            Point coordinates = GetMousePosition(sender);

            Rectangle pixel = SetPixel((int)coordinates.X, (int)coordinates.Y, _defaultPolygonColor);
            pixel.ContextMenu = _verticleContextMenu;
            _currentPolygon.AddNewVertex(coordinates, pixel);

            if (_currentPolygon.Vertexes.Count >= 2)
            {
                int index = _currentPolygon.Vertexes.Count - 2;
                Vertex vertexOne = _currentPolygon.GetVertexByIndex(index);
                Vertex vertexTwo = _currentPolygon.GetVertexByIndex(index + 1);
                _currentPolygon.AddNewEdge(vertexOne,vertexTwo, 
                    CreateEdgeLine(vertexOne, vertexTwo));
            }
        }

        private void EndPolygon(object sender, RoutedEventArgs e)
        {
            if (_currentPolygon.Vertexes.Count < 3)
                return;

            Rectangle pix;
            if (CheckMenuItem(sender, out pix))
            {
                Vertex endVerticle = _currentPolygon.GetVertexByPixel(pix);
                Vertex lastVerticle = _currentPolygon.GetLastVertex();

                if(Math.Abs(endVerticle.Id - lastVerticle.Id) <= 1)
                    return;

                _currentPolygon.AddNewEdge(lastVerticle, endVerticle,
                    CreateEdgeLine(lastVerticle, endVerticle));

                DeleteTail(endVerticle.Id);
                DrawPolygonToggleButton.IsChecked = false;               
            }
        }

        private void DisableSettingVerticles()
        {
            Canvas.MouseLeftButtonDown -= SetVerticle;
            foreach (var vertex in _currentPolygon.Vertexes)
            {
                Canvas.Children.Remove(vertex.Pixel);
                vertex.Pixel = new Rectangle();
            }
        }

        private bool CheckMenuItem(object menuItemToCheck, out Rectangle rc)
        {
            MenuItem mi = menuItemToCheck as MenuItem;
            if (mi != null)
            {
                rc = ((ContextMenu)mi.Parent).PlacementTarget as Rectangle;
                return true;
            }

            rc = new Rectangle();
            return false;
        }

        private Rectangle SetPixel(int x, int y, Brush color, int size = 10)
        {
            Rectangle rectangle = new Rectangle() { Width = size, Height = size, Fill = color };
            Canvas.Children.Add(rectangle);
            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);
            return rectangle;
        }

        private void DeleteTail(int endVerticleId)
        {
            List<Vertex> vertexesToDelete = new List<Vertex>(_currentPolygon.Vertexes.Where(v => v.Id < endVerticleId));
            List<Edge> edgesToDelete = new List<Edge>(_currentPolygon.Edges.Where(e => e.VertexOne.Id < endVerticleId || e.VertexTwo.Id < endVerticleId));

            foreach (var vertex in vertexesToDelete)
            {
                Canvas.Children.Remove(vertex.Pixel);
                _currentPolygon.Vertexes.Remove(vertex);
            }
            foreach (var edge in edgesToDelete)
            {
                foreach (var linePixel in edge.Line)
                {
                    Canvas.Children.Remove(linePixel.Rectangle);
                }
                _currentPolygon.Edges.Remove(edge);
            }
        }

        private void ClearCanvas(object sender, RoutedEventArgs e)
        {
            Canvas.Children.Clear();
            _polygons.Clear();
        }

        private void StartDrawingPolygon(object sender, RoutedEventArgs e)
        {
            _currentPolygon = new Polygon();
            Canvas.MouseLeftButtonDown += SetVerticle;
        }

        private void EndDrawingPolygon(object sender, RoutedEventArgs e)
        {
            DisableSettingVerticles();

            if (_currentPolygon.Vertexes.Count <= 2)
            {
                foreach (var edge in _currentPolygon.Edges)
                {
                    foreach (var linePixel in edge.Line)
                    {
                        Canvas.Children.Remove(linePixel.Rectangle);
                    }                   
                }
                _currentPolygon = new Polygon();
                return;
            }

            if (_currentPolygon.Vertexes.Count != _currentPolygon.Edges.Count)
            {
                Vertex firstVertex = _currentPolygon.GetFirstVertex();
                Vertex lastVertex = _currentPolygon.GetLastVertex();

                _currentPolygon.AddNewEdge(lastVertex, firstVertex,
                    CreateEdgeLine(lastVertex, firstVertex));
            }

            _currentPolygon.InitializeEdgeTable();
            _polygons.Add(_currentPolygon);
            _currentPolygon = new Polygon();

            ColorPolygonEdges(_selectedPolygon, _defaultPolygonColor);
            _selectedPolygonIndex = _polygons.Count - 1;
            _selectedPolygon = _polygons[_selectedPolygonIndex];
            ColorPolygonEdges(_selectedPolygon, _defaultSelectedPolygonColor);
        }

        private void ColorPolygonClick(object sender, RoutedEventArgs e)
        {
            if(_selectedPolygon == null) return;

            Polygon polygon = _selectedPolygon;

            if (!_isColorInsteadOfTexture)
            {
                _fillPolygonTexture = ConvertImageToBitmap(_fillPolygonTextureBitmapImage, (int)Canvas.ActualHeight, (int)Canvas.ActualWidth);
            }
            Brush color = LambertFormula();            

            List <EdgeTableElem> activeEdgeTable = new List<EdgeTableElem>();

            for (int y = polygon.YMin; y < polygon.YMax + 1; y+= 4)
            {
                if (polygon.EdgeTable[y].Count != 0)
                {
                    activeEdgeTable = polygon.EdgeTable[y].OrderBy(el => el.X).ToList();
                }
                polygon.PixelFill.Add(FillScanLineWithColor(activeEdgeTable, y,color));
            }

        }

        private List<Rectangle> FillScanLineWithColor(List<EdgeTableElem> activeEdgeTable, int y, Brush color)
        {
            var retValue = new List<Rectangle>();

            for (int i = 0; i < activeEdgeTable.Count - 1; i += 2)
            {
                retValue.AddRange(ColorPartOfScanLine(activeEdgeTable[i].Table[y], activeEdgeTable[i + 1].Table[y], y, color));
            }

            return retValue;
        }

        private List<Rectangle> ColorPartOfScanLine(int xLeft, int xRight, int y,Brush color)
        {
            var retValue = new List<Rectangle>();
            int x = xLeft;

            while (x <= xRight)
            {
                if (_isColorInsteadOfTexture)
                {
                    retValue.Add(SetPixel(x, y, color, 5));
                }
                else
                {
                    retValue.Add(SetPixel(x, y, GetTexturePixel(x,y) , 5));
                }
                
                x+= 4;
            }
            return retValue;
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

        private void SelectPrevPolygon(object sender, RoutedEventArgs e)
        {
            if(_polygons.Count == 0) return;

            if (_selectedPolygonIndex > 0)
            {
                ColorPolygonEdges(_selectedPolygon, _defaultPolygonColor);
                _selectedPolygonIndex -= 1;
                _selectedPolygon = _polygons[_selectedPolygonIndex];
                ColorPolygonEdges(_selectedPolygon, _defaultSelectedPolygonColor);
            }
        }

        private void SelectNextPolygon(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count == 0) return;

            if (_selectedPolygonIndex < _polygons.Count - 1)
            {
                ColorPolygonEdges(_selectedPolygon, _defaultPolygonColor);
                _selectedPolygonIndex += 1;
                _selectedPolygon = _polygons[_selectedPolygonIndex];
                ColorPolygonEdges(_selectedPolygon, _defaultSelectedPolygonColor);
            }
        }

        private void ColorPolygonEdges(Polygon polygon, Brush color)
        {
            if(polygon == null) return;

            foreach (var edge in polygon.Edges)
            {
                foreach (var linePixel in edge.Line)
                {
                    linePixel.Rectangle.Fill = color;
                }
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

        private void SortVertexesClockwise(List<Vertex> vertexesList)
        {
            int x = int.MaxValue;
            int y = int.MaxValue;
            Vertex v = vertexesList.FirstOrDefault();
            foreach (var vertex in vertexesList)
            {
                if (vertex.X < x || vertex.Y < y)
                {
                    v = vertex;
                    x = vertex.X;
                    y = vertex.Y;
                }
            }

            int index = vertexesList.IndexOf(v);
            int nextIndex = index + 1;

            if (nextIndex > vertexesList.Count - 1)
            {
                nextIndex = 0;
            }

            Vertex v1 = vertexesList[index];
            Vertex v2 = vertexesList[nextIndex];

            if (Det(v1, v2) < 0)
            {
                vertexesList.Reverse();
            }
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

        private void GenerateListsWithIntersectionPoints(Polygon polygonOne, Polygon polygonTwo, out List<Vertex> vertexesOne, out List<Vertex> vertexesTwo)
        {
            vertexesOne = new List<Vertex>(polygonOne.Vertexes);
            vertexesTwo = new List<Vertex>(polygonTwo.Vertexes);

            foreach (var edgeOne in polygonOne.Edges)
            {
                foreach (var edgeTwo in polygonTwo.Edges)
                {
                    if (CheckIfCanIntersect(edgeOne, edgeTwo))
                    {
                        Point coordinates = GetIntersectionCoordinates(edgeOne, edgeTwo);

                        Vertex vOne = new Vertex(vertexesOne.Count, coordinates, new Rectangle());
                        vOne.IsIntersected = true;
                        int indexOne = vertexesOne.IndexOf(edgeOne.VertexTwo);
                        vertexesOne.Insert(indexOne, vOne);

                        Vertex vTwo = new Vertex(vertexesTwo.Count, coordinates, new Rectangle());
                        vTwo.IsIntersected = true;
                        int indexTwo = vertexesTwo.IndexOf(edgeTwo.VertexTwo);
                        vertexesTwo.Insert(indexTwo, vTwo);
                    }
                }
            }

            SortVertexesClockwise(vertexesOne); // naprawić to 
            SortVertexesClockwise(vertexesTwo);
        }

        private void StartClippingPolygons(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count < 2) return;

            ColorPolygonEdges(_selectedPolygon, _defaultPolygonColor);
            ClippingPolygonsStackPanel.Visibility = Visibility.Visible;

            _selectedClippingPolygonOne = _polygons[0];
            _selectedClippingPolygonOneIndex = 0;
            ColorPolygonEdges(_selectedClippingPolygonOne, _defaultSelectedClippingPolygonOneColor);

            _selectedClippingPolygonTwo = _polygons[1];
            _selectedClippingPolygonTwoIndex = 1;
            ColorPolygonEdges(_selectedClippingPolygonTwo, _defaultSelectedClippingPolygonTwoColor);
        }

        private void EndClippingPolygons(object sender, RoutedEventArgs e)
        {
            ClippingPolygonsStackPanel.Visibility = Visibility.Collapsed;
            ColorPolygonEdges(_selectedClippingPolygonOne, _defaultPolygonColor);
            ColorPolygonEdges(_selectedClippingPolygonOne, _defaultPolygonColor);
            if (_polygons.Count != 0)
            {
                _selectedPolygon = _polygons[0];
                ColorPolygonEdges(_selectedPolygon, _defaultSelectedPolygonColor);
            }
        }

        private void SelectPreviousClippingPolygonOne(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count < 2) return;

            if (_selectedClippingPolygonOneIndex > 0)
            {
                ColorPolygonEdges(_selectedClippingPolygonOne, _defaultPolygonColor);
                _selectedClippingPolygonOneIndex -= 1;
                if (_selectedClippingPolygonTwoIndex == _selectedClippingPolygonOneIndex) return;
                _selectedClippingPolygonOne = _polygons[_selectedClippingPolygonOneIndex];
                ColorPolygonEdges(_selectedClippingPolygonOne, _defaultSelectedClippingPolygonOneColor);
            }
        }

        private void SelectNextClippingPolygonOne(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count < 2) return;

            if (_selectedClippingPolygonOneIndex < _polygons.Count - 1)
            {
                ColorPolygonEdges(_selectedClippingPolygonOne, _defaultPolygonColor);
                _selectedClippingPolygonOneIndex += 1;
                _selectedClippingPolygonOne = _polygons[_selectedClippingPolygonOneIndex];
                ColorPolygonEdges(_selectedClippingPolygonOne, _defaultSelectedPolygonColor);
            }
        }

        private void SelectPreviousClippingPolygonTwo(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count < 2) return;

            if (_selectedClippingPolygonTwoIndex > 0)
            {
                ColorPolygonEdges(_selectedClippingPolygonTwo, _defaultPolygonColor);
                _selectedClippingPolygonTwoIndex -= 1;
                if (_selectedClippingPolygonTwoIndex == _selectedClippingPolygonOneIndex) return;
                _selectedClippingPolygonTwo = _polygons[_selectedClippingPolygonTwoIndex];
                ColorPolygonEdges(_selectedClippingPolygonTwo, _defaultSelectedPolygonColor);
            }
        }

        private void SelectNextClippingPolygonTwo(object sender, RoutedEventArgs e)
        {
            if (_polygons.Count < 2) return;

            if (_selectedClippingPolygonTwoIndex < _polygons.Count - 1)
            {
                ColorPolygonEdges(_selectedClippingPolygonTwo, _defaultPolygonColor);
                _selectedClippingPolygonTwoIndex += 1;
                _selectedClippingPolygonTwo = _polygons[_selectedClippingPolygonTwoIndex];
                ColorPolygonEdges(_selectedClippingPolygonTwo, _defaultSelectedPolygonColor);
            }
        }

        private void ClipPolygons(object sender, RoutedEventArgs e)
        {
            if (_selectedClippingPolygonOne == null || _selectedClippingPolygonTwo == null) return;

            List<Vertex> clipPolygonOneVertexes = new List<Vertex>();
            List<Vertex> clipPolygonTwoVertexes = new List<Vertex>();

            GenerateListsWithIntersectionPoints(_selectedClippingPolygonOne, _selectedClippingPolygonTwo, out clipPolygonOneVertexes, out clipPolygonTwoVertexes);

            _polygons.Remove(_selectedClippingPolygonOne);
            ErasePolygonFromCanvas(_selectedClippingPolygonOne);
            _polygons.Remove(_selectedClippingPolygonTwo);
            ErasePolygonFromCanvas(_selectedClippingPolygonTwo);

            if (clipPolygonOneVertexes.Count != _selectedClippingPolygonOne.Vertexes.Count)
            {
                Vertex startVertex;
                while (CheckIfIsaAnyUnvisitedIntersectionPoint(clipPolygonOneVertexes, out startVertex))
                {
                    List<Point> newPolygonPointsCoordinates = CreateClippedPolygon(clipPolygonOneVertexes, clipPolygonTwoVertexes, startVertex);
                    CreateAndDrawNewPolygon(newPolygonPointsCoordinates);
                }
            }

            if (_polygons.Count < 2)
            {

                _selectedClippingPolygonOne = null;
                _selectedClippingPolygonTwo = null;
                ClipPolygonsToggleButton.IsChecked = false;
            }
            else
            {
                ClipPolygonsToggleButton.IsChecked = true;
            }



        }

        private void CreateAndDrawNewPolygon(List<Point> pointsCoordinates)
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
            _polygons.Add(newPolygon);
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

        private bool CheckIfIsaAnyUnvisitedIntersectionPoint(List<Vertex> vertexes, out Vertex notVisitedVertex)
        {
            notVisitedVertex = new Vertex();
            foreach (var vertex in vertexes)
            {
                if (vertex.IsIntersected && !vertex.IsVisited)
                {
                    notVisitedVertex = vertex;
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void ErasePolygonFromCanvas(Polygon polygon)
        {
            foreach (var edge in polygon.Edges)
            {
                foreach (var linePixel in edge.Line)
                {
                    Canvas.Children.Remove(linePixel.Rectangle);
                }
            }
        }

        private Brush GetCurrentPixelColor(int x, int y)
        {



            return new SolidColorBrush();
        }

        private Brush LambertFormula()
        {
            int rgbCount = 255;

            Vector objectColorVector = new Vector(_fillColor);
            Vector lightColorVector = new Vector(_lightColor);

            Vector normalAddedWithDisurbVector = _normalVector.AddVectors(_disturbVector);
            Vector normalDisurbVector = normalAddedWithDisurbVector.Normalize();

            double cos = normalDisurbVector.DotProduct(_lightVersor);

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

        private void LightColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (LightColorPicker.SelectedColor != null)
            {
                _lightColor = new SolidColorBrush((Color)LightColorPicker.SelectedColor);
            }
        }

        public void LoadFillPolygonTexture(object sender, RoutedEventArgs e)
        {
            

        }

        private void ChooseColorFillPolygon(object sender, RoutedEventArgs e)
        {
            _isColorInsteadOfTexture = true;
        }

        private void ChooseTextureFillPolygon(object sender, RoutedEventArgs e)
        {
            _isColorInsteadOfTexture = false;
        }
    }
}
