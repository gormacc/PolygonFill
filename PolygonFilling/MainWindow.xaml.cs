using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PolygonFilling.Structures;
using Polygon = PolygonFilling.Structures.Polygon;

namespace PolygonFilling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// trzeba poprawić sortowanie kubełkowe !!!!
    /// 
    /// </summary>
    public partial class MainWindow
    {
        private readonly List<Polygon> _polygons = new List<Polygon>();
        private Polygon _currentPolygon = new Polygon();
        private Polygon _selectedPolygon = new Polygon();
        private int _selectedPolygonIndex;
        private readonly Brush _defaultPolygonColor = Brushes.Black;
        private readonly ContextMenu _verticleContextMenu = new ContextMenu();
        private Brush _fillColor = Brushes.Red;
        private readonly Brush _defaultSelectedPolygonColor = Brushes.Green;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVerticleContextMenu();
        }

        private void InitializeVerticleContextMenu()
        {
            MenuItem endDrawingPolygonMenuItem = new MenuItem { Header = "Zakoncz rysowanie wielokata" };
            endDrawingPolygonMenuItem.Click += EndPolygon;

            _verticleContextMenu.Items.Add(endDrawingPolygonMenuItem);
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

            if (CheckMenuItem(sender, out var pix))
            {
                Vertex endVerticle = _currentPolygon.GetVertexByPixel(pix);
                Vertex lastVerticle = _currentPolygon.GetLastVertex();

                if(Math.Abs(endVerticle.Id - lastVerticle.Id) <= 1)
                    return;

                _currentPolygon.AddNewEdge(endVerticle, lastVerticle,
                    CreateEdgeLine(endVerticle, lastVerticle));

                DeleteTail(endVerticle.Id);
                DrawPolygonToggleButton.IsChecked = false;               
            }
        }

        private void DisableSettingVerticles()
        {
            canvas.MouseLeftButtonDown -= SetVerticle;
            foreach (var vertex in _currentPolygon.Vertexes)
            {
                canvas.Children.Remove(vertex.Pixel);
                vertex.Pixel = new Rectangle();
            }
        }

        private bool CheckMenuItem(object menuItemToCheck, out Rectangle rc)
        {
            if (menuItemToCheck is MenuItem mi)
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
            canvas.Children.Add(rectangle);
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
                canvas.Children.Remove(vertex.Pixel);
                _currentPolygon.Vertexes.Remove(vertex);
            }
            foreach (var edge in edgesToDelete)
            {
                foreach (var linePixel in edge.Line)
                {
                    canvas.Children.Remove(linePixel.Rectangle);
                }
                _currentPolygon.Edges.Remove(edge);
            }
        }

        private void ClearCanvas(object sender, RoutedEventArgs e)
        {
            canvas.Children.Clear();
            _polygons.Clear();
        }

        private void StartDrawingPolygon(object sender, RoutedEventArgs e)
        {
            _currentPolygon = new Polygon();
            canvas.MouseLeftButtonDown += SetVerticle;
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
                        canvas.Children.Remove(linePixel.Rectangle);
                    }                   
                }
                _currentPolygon = new Polygon();
                return;
            }

            if (_currentPolygon.Vertexes.Count != _currentPolygon.Edges.Count)
            {
                Vertex firstVertex = _currentPolygon.GetFirstVertex();
                Vertex lastVerticle = _currentPolygon.GetLastVertex();

                _currentPolygon.AddNewEdge(firstVertex, lastVerticle,
                    CreateEdgeLine(firstVertex, lastVerticle));
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
            Polygon polygon = _selectedPolygon;
            List<EdgeTableElem> activeEdgeTable = new List<EdgeTableElem>();

            for (int y = polygon.YMin; y < polygon.YMax + 1; y++)
            {
                if (polygon.EdgeTable[y].Count != 0)
                {
                    activeEdgeTable = polygon.EdgeTable[y].OrderBy(el => el.X).ToList();
                }
                polygon.PixelFill.Add(FillScanLineWithColor(activeEdgeTable, y, _fillColor));
            }

        }

        private List<Rectangle> FillScanLineWithColor(List<EdgeTableElem> activeEdgeTable, int y, Brush color)
        {
            var retValue = new List<Rectangle>();
            if (activeEdgeTable.Count % 2 == 0)
            {
                for (int i = 0; i < activeEdgeTable.Count - 1; i += 2)
                {
                    retValue.AddRange(ColorPartOfScanLine(activeEdgeTable[i].Table[y], activeEdgeTable[i + 1].Table[y], y, color));
                }
            }
            else
            {
                for (int i = 0; i < activeEdgeTable.Count - 1; i += 2)
                {
                    retValue.AddRange(ColorPartOfScanLine(activeEdgeTable[i].X, activeEdgeTable[i + 1].X, y, color));
                }
                retValue.AddRange(ColorPartOfScanLine(activeEdgeTable[activeEdgeTable.Count - 1].Table[y], activeEdgeTable[activeEdgeTable.Count - 1].Table[y], y, color));
            }
            return retValue;
        }

        private List<Rectangle> ColorPartOfScanLine(int xLeft, int xRight, int y, Brush color)
        {
            var retValue = new List<Rectangle>();
            double x = xLeft;
            while (x <= xRight)
            {
                retValue.Add(SetPixel((int)x, y, color, 3));
                x++;
            }
            return retValue;
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

        private void PickedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
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
            foreach (var edge in polygon.Edges)
            {
                foreach (var linePixel in edge.Line)
                {
                    linePixel.Rectangle.Fill = color;
                }
            }
        }

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

            if (Det(v1,v2) < 0)
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

        private void GenerateListsWithIntersectionPoints(Polygon polygonOne, Polygon polygonTwo , out List<Vertex> vertexesOne, out List<Vertex> vertexesTwo)
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
                        Vertex firstVertexOne = edgeOne.VertexOne.Id < edgeOne.VertexTwo.Id ? edgeOne.VertexOne : edgeOne.VertexTwo;
                        int indexOne = vertexesOne.IndexOf(firstVertexOne);
                        vertexesOne.Insert(indexOne, vOne);

                        Vertex vTwo = new Vertex(vertexesTwo.Count, coordinates, new Rectangle());
                        Vertex firstVertexTwo = edgeTwo.VertexOne.Id < edgeTwo.VertexTwo.Id ? edgeTwo.VertexOne : edgeTwo.VertexTwo;
                        int indexTwo = vertexesTwo.IndexOf(firstVertexTwo);
                        vertexesOne.Insert(indexTwo, vTwo);
                    }
                }
            }

            SortVertexesClockwise(vertexesOne);
            SortVertexesClockwise(vertexesTwo);
        }
    }
}
