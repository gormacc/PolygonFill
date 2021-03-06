﻿using System;
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
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Polygon> _polygons = new List<Polygon>();
        private Polygon _currentPolygon = new Polygon();
        private readonly Brush _defaultPolygonColor = Brushes.Black;
        private readonly ContextMenu _verticleContextMenu = new ContextMenu();

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

            Rectangle pixel = SetPixel((int)coordinates.X, (int)coordinates.Y);
            pixel.ContextMenu = _verticleContextMenu;
            _currentPolygon.AddNewVertex(coordinates, pixel);

            if (_currentPolygon.Vertexes.Count >= 2)
            {
                int index = _currentPolygon.Vertexes.Count - 2;
                Vertex vertexOne = _currentPolygon.GetVertexByIndex(index);
                Vertex vertexTwo = _currentPolygon.GetVertexByIndex(index + 1);
                _currentPolygon.AddNewEdge(vertexOne,vertexTwo, 
                    CreateEdgeLine(vertexOne.Coordinates, vertexTwo.Coordinates));
            }
        }

        private Line CreateEdgeLine(Point p1, Point p2)
        {
            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = _defaultPolygonColor,
                StrokeThickness = 4
            };

            canvas.Children.Add(line);

            return line;
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

                _currentPolygon.AddNewEdge(endVerticle, lastVerticle,
                    CreateEdgeLine(endVerticle.Coordinates, lastVerticle.Coordinates));

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
            MenuItem mi = menuItemToCheck as MenuItem;
            if (mi != null)
            {
                rc = ((ContextMenu)mi.Parent).PlacementTarget as Rectangle;
                return true;
            }

            rc = new Rectangle();
            return false;
        }

        private Rectangle SetPixel(int x, int y, int size = 10)
        {
            Rectangle rectangle = new Rectangle() { Width = size, Height = size, Fill = _defaultPolygonColor };
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
                canvas.Children.Remove(edge.Lin);
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
                    canvas.Children.Remove(edge.Lin);
                    _currentPolygon = new Polygon();
                    return;
                }
            }

            if (_currentPolygon.Vertexes.Count != _currentPolygon.Edges.Count)
            {
                Vertex firstVertex = _currentPolygon.GetFirstVertex();
                Vertex lastVerticle = _currentPolygon.GetLastVertex();

                _currentPolygon.AddNewEdge(firstVertex, lastVerticle,
                    CreateEdgeLine(firstVertex.Coordinates, lastVerticle.Coordinates));
            }

            _polygons.Add(_currentPolygon);
            _currentPolygon = new Polygon();
        }
    }
}
