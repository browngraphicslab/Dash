﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash
{
    public class LassoSelectHelper
    {

        private List<Point> _points;
        private int _min;
        private Point _rootPoint;
        private SortedDictionary<double, Point> _sortedPoints;
        private Stack<Point> _hullPoints;
        private CollectionFreeformView _view;
        private Polygon _hull;
        private Polygon _visualHull;
        private MenuFlyout _menu = new MenuFlyout();
        private Grid _flyoutBase;

        public LassoSelectHelper(CollectionFreeformView view)
        {
            _view = view;
            //var delete = new MenuFlyoutItem {Text = "Delete"};
            //delete.Tapped += DeleteOnTapped;
            //_menu.Items.Add(delete);
            //_menu.Placement = FlyoutPlacementMode.Bottom;
        }

        private void DeleteOnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            if (_view.xItemsControl.ItemsPanelRoot.Children.Contains(_visualHull))
                _view.xItemsControl.ItemsPanelRoot.Children.Remove(_visualHull);
            foreach (var doc in _view.ViewModel.SelectionGroup.Select(doc => doc.Controller))
            {
                _view.ViewModel.RemoveDocument(doc);
            }
        }

        /// <summary>
        /// Executes hull tasks, i.e. sorting points and figuring out the hull. 
        /// </summary>
        public List<DocumentViewModelParameters> GetSelectedDocuments(List<Point> points)
        {
            if (_view.xItemsControl.ItemsPanelRoot.Children.Contains(_visualHull))
                _view.xItemsControl.ItemsPanelRoot.Children.Remove(_visualHull);
            _points = points;
            FindBottomLeftMostPoint();
            PlaceBottomLeftMostPointAtFirstPosition();
            SortPoints();
            FigureOutConvexHull();

            // only make a hull if we have some points
            if (_hullPoints != null )
            {
                AddSelectionHull();
                var selected = SelectContainedNodes(new List<DocumentViewModelParameters>(_view.ViewModel.DocumentViewModels));
                if (selected.Count > 0)
                {
                    AddVisualHull();
                    return selected;
                }
            }

            return new List<DocumentViewModelParameters>();
        }

        private void FindBottomLeftMostPoint()
        {
            double yMin = _points[0].Y;
            _min = 0;

            // loop thru all points
            for (int i = 1; i < _points.Count(); i++)
            {
                double y = _points[i].Y;
                // pick the bottom-most or choose the left mostt point in case of a tie
                if ((y > yMin) || (y == yMin && _points[i].X < _points[_min].X))
                {
                    yMin = _points[i].Y;
                    _min = i;
                }
            }
        }
   

        // swaps the bottom left point to be the first position
        private void PlaceBottomLeftMostPointAtFirstPosition()
        {
            this.Swap(_points[0], _points[_min]);
            _rootPoint = _points[0];
        }

        // swaps two points in the points list
        private void Swap(Point one, Point two)
        {
            int indexOne = _points.IndexOf(one);
            int indexTwo = _points.IndexOf(two);

            var temp = one;
            _points[indexOne] = _points[indexTwo];
            _points[indexTwo] = temp;

        }

        // sorts all of the points by polar angle in counterclockwise order around the bottom-left-most point
        private void SortPoints()
        {
            _sortedPoints = new SortedDictionary<double, Point>();
            for (int i = 0; i < _points.Count(); i++)
            {
                var point = _points[i];
                double quant = QuantifyAngle(point);

                // If the list doesn't have a pointat that angle yet, then add it
                if (!_sortedPoints.ContainsKey(quant))
                {
                    _sortedPoints.Add(quant, point);
                }


                // if there are multiple points with the same angle, keep the one in that is furthest away from the root point (but you shouldn't delete the root point)
                else if (_sortedPoints.ContainsKey(quant) && _sortedPoints[quant] != _rootPoint)
                {
                    // distance of point already in the dictionary
                    double d1 = DistanceToRootPoint(_sortedPoints[quant]);
                    // distance of the new point to the root point
                    double d2 = DistanceToRootPoint(point);

                    // if the new point is farther away than the point already in the dictionary
                    if (d2 > d1)
                    {
                        _sortedPoints.Remove(quant);
                        _sortedPoints.Add(quant, point);
                    }
                }
            }
        }

        // quantifies the polar angle based on the slope. For example, a quanitification of negative infinity would be an angle of 0, wherease a quantification of positive infinity would be an angle of 180 degrees
        private double QuantifyAngle(Point point)
        {
            double quant = -(point.X - _rootPoint.X) / (_rootPoint.Y - point.Y);
            return quant;
        }

        // returns square of distance to the root point
        private double DistanceToRootPoint(Point point)
        {
            return (point.X - _rootPoint.X) * (point.X - _rootPoint.X) + (point.Y - _rootPoint.Y) * (point.Y - _rootPoint.Y);
        }

        // figures out the points in the convex hull and stores it as a list
        private void FigureOutConvexHull()
        {
            List<Point> sortedPoints = new List<Point>(_sortedPoints.Values);
            if (sortedPoints.Count < 5)
                return;
            _hullPoints = new Stack<Point>();
            for (int i = 0; i < 3; i++)
            {
                _hullPoints.Push(sortedPoints[i]);
            }

            // process remaining points
            for (int i = 3; i < sortedPoints.Count(); i++)
            {
                // keep removing the top while the angle formed by points next to top, top, and the point at index i makes a non-left turn
                while (Orientation(NextToTop(), _hullPoints.Peek(), sortedPoints[i]) != 2)
                {
                    if (_hullPoints.Count() < 3) return;
                    _hullPoints.Pop();

                }
                _hullPoints.Push(sortedPoints[i]);
            }
        }

        // returns the point right below the top-most point in the hull stack
        private Point NextToTop()
        {
            var top = _hullPoints.Pop();
            var nextToTop = _hullPoints.Peek();
            _hullPoints.Push(top);
            return nextToTop;
        }

        // finds the orientation of the triplet (p,q,r)
        // returns 0 if collinear, 1 of clockwise, 2 if counterclockwise
        private int Orientation(Point r, Point q, Point p)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (val == 0)
            {
                return 0;
            }
            else if (val > 0)
            {
                return 1; //clockwise
            }
            else
            {
                return 2; //counter clockwise
            }
        }

        /// <summary>
        /// Note: The _hull is represents the selection hull in global space (absolute coordinates, i.e. x=50,000, y = 50,000).
        /// The _visualHull represents the selection hull in local space, and you can add the visual hull to the main canvas to actually
        /// see the hull you have drawn.
        /// </summary>
        private void AddSelectionHull()
        {
            _hull = new Polygon();
            _visualHull = new Polygon();

            // give both hulls the proper points
            while (_hullPoints.Count() > 0)
            {
                var point = _hullPoints.Pop();
                _visualHull.Points.Add(point);
                _hull.Points.Add(point);
            }

            
        }

        private void AddVisualHull()
        {
            _flyoutBase = new Grid {Width = 1, Height = 1};
            // format visual hull
            _visualHull.Fill = (SolidColorBrush)Application.Current.Resources["WindowsBlue"];
            _visualHull.Opacity = .1;
            _visualHull.Stroke = (SolidColorBrush)Application.Current.Resources["WindowsBlue"];
            _visualHull.StrokeThickness = 1;
            _visualHull.ManipulationMode = ManipulationModes.All;
            _visualHull.ManipulationDelta += VisualHullOnManipulationDelta;
            _visualHull.CompositeMode = ElementCompositeMode.SourceOver;
            //_visualHull.Tapped += VisualHullOnTapped;
            (_view.xItemsControl.ItemsPanelRoot as Canvas).Children.Add(_visualHull);
        }

        private void VisualHullOnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (!_view.xItemsControl.ItemsPanelRoot.Children.Contains(_flyoutBase))
                _view.xItemsControl.ItemsPanelRoot.Children.Add(_flyoutBase);
            Canvas.SetLeft(_flyoutBase, e.GetPosition(_view.xItemsControl.ItemsPanelRoot).X);
            Canvas.SetTop(_flyoutBase, e.GetPosition(_view.xItemsControl.ItemsPanelRoot).Y);
            _menu.ShowAt(_flyoutBase);
        }

        private void VisualHullOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = new Point(e.Delta.Translation.X / _view.Zoom, e.Delta.Translation.Y / _view.Zoom);
            var pos = new Point(Canvas.GetLeft(_visualHull), Canvas.GetTop(_visualHull));
            Canvas.SetLeft(_visualHull, pos.X + delta.X);
            Canvas.SetTop(_visualHull, pos.Y + delta.Y);
            foreach (var doc in _view.ViewModel.SelectionGroup)
            {
                var docPos = doc.Controller.GetPositionField().Data;
                docPos.X += delta.X;
                docPos.Y += delta.Y;
                doc.Controller.GetPositionField().Data = docPos;
            }
            e.Handled = true;
        }

        // selects contained atoms by figuring out the atoms in the selection hull
        private List<DocumentViewModelParameters> SelectContainedNodes(List<DocumentViewModelParameters> docVMs)
        {
            var selectedDocs = new List<DocumentViewModelParameters>();
            foreach (var docVM in docVMs)
            {
                var doc = docVM.Controller;
                var topLeft = doc.GetPositionField(null).Data;
                var width = doc.GetWidthField(null).Data;
                var height = doc.GetHeightField(null).Data;
                var points = new List<Point>
                {
                    topLeft,
                    new Point(topLeft.X + width, topLeft.Y),
                    new Point(topLeft.X + width, topLeft.Y + height),
                    new Point(topLeft.X, topLeft.Y + height)

                };
                bool inHull = false;
                foreach (var refPoint in points)
                {
                    if (this.IsPointInHull(refPoint))
                    {
                        inHull = true;
                    }
                }
                if (inHull)
                {
                    selectedDocs.Add(docVM);
                }
            }
            return selectedDocs;
        }

        private bool IsPointInHull(Point testPoint)
        {
            bool result = false;
            var polygon = _hull.Points;

            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++) //loop thru all points in the convex hull
            {

                //if the test point is below the polygon point and above the  last polygon point OR if the testpoin is below the previous polygon point and above the current polygon pt.
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }
}
