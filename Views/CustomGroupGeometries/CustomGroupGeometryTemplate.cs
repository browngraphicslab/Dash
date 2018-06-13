using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Dash
{
    public static class CustomGroupGeometryTemplate
    {
        public static PathGeometry RoundedRectangle()
        {
            var roundedPathSegs = new PathSegmentCollection
            {
                new ArcSegment { Size = new Size(15, 15), Point = new Point(0, 15) },
                new LineSegment { Point = new Point(0, 150) },
                new ArcSegment { Size = new Size(15, 15), Point = new Point(15, 165) },
                new LineSegment { Point = new Point(150, 165) },
                new ArcSegment { Size = new Size(15, 15), Point = new Point(165, 150) },
                new LineSegment { Point = new Point(165, 15) },
                new ArcSegment { Size = new Size(15, 15), Point = new Point(150, 0) },
            };
            var roundedFigure = new PathFigure { StartPoint = new Point(15, 0), Segments = roundedPathSegs };

            return new PathGeometry { Figures = new PathFigureCollection { roundedFigure } };
        }

        public static PathGeometry RoundedFrame()
        {
            var frame = RoundedRectangle();

            var internalCutoutSegs = new PathSegmentCollection
            {
                new LineSegment { Point = new Point(15, 150) },
                new LineSegment { Point = new Point(150, 150) },
                new LineSegment { Point = new Point(150, 15) },
            };
            var internalCutout = new PathFigure { StartPoint = new Point(15, 15), Segments = internalCutoutSegs };

            frame.Figures.Add(internalCutout);
            return frame;
        }

        public static PathGeometry LinearPolygon(int numSides)
        {
            if (numSides.GetType() != typeof(int) || numSides < 5) return null;

            var points = CalculateVertices(numSides, 100, 90, new Point(100, 100));
            var smallestX = float.PositiveInfinity;

            var polyPathSegs = new PathSegmentCollection();
            var polygon = new PathFigure { Segments = polyPathSegs };

            foreach (var pt in points) if (pt.X < smallestX) smallestX = (float)pt.X;
            for (int i = 0; i < points.Count; i++)
            {
                var thisPoint = points[i];
                points[i] = new Point(thisPoint.X - smallestX, thisPoint.Y);
            }

            polygon.StartPoint = points.First();
            points.RemoveAt(0);

            foreach (var pt in points) polyPathSegs.Add(new LineSegment { Point = pt });

            return new PathGeometry { Figures = new PathFigureCollection { polygon } };
        }

        private static List<Point> CalculateVertices(int sides, int radius, int startingAngle, Point center)
        {
            var points = new List<Point>();
            var step = 360.0f / sides;

            float angle = startingAngle; //starting angle
            for (double i = startingAngle; i < startingAngle + 360.0; i += step) //go in a full circle
            {
                points.Add(DegreesToPoint(angle, radius, center)); //code snippet from above
                angle += step;
            }

            return points;
        }

        private static Point DegreesToPoint(float degrees, float radius, Point origin)
        {
            var xy = new Point();
            var radians = degrees * Math.PI / 180.0;

            xy.X = (float)Math.Cos(radians) * radius + origin.X;
            xy.Y = (float)Math.Sin(-radians) * radius + origin.Y;

            return xy;
        }

        public static PathGeometry Clover()
        {
            var topLeaf = new PathFigure { StartPoint = new Point(30, 40), Segments = new PathSegmentCollection { new ArcSegment { Size = new Size(15, 15), Point = new Point(30, 0) } } };
            var bottomLeaf = new PathFigure { StartPoint = new Point(30, 40), Segments = new PathSegmentCollection { new ArcSegment { Size = new Size(15, 15), Point = new Point(30, 80) } } };
            var leftLeaf = new PathFigure { StartPoint = new Point(30, 40), Segments = new PathSegmentCollection { new ArcSegment { Size = new Size(15, 15), Point = new Point(0, 40) } } };
            var rightLeaf = new PathFigure { StartPoint = new Point(30, 40), Segments = new PathSegmentCollection { new ArcSegment { Size = new Size(15, 15), Point = new Point(60, 40) } } };

            return new PathGeometry { Figures = new PathFigureCollection { topLeaf, bottomLeaf, leftLeaf, rightLeaf } };
        }
    }
}
