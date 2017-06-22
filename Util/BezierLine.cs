using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Dash {
    /// <summary>
    /// Generates a bezier-curved path between two points that can be drawn
    /// onto a XAML canvas.
    /// </summary>
    class BezierLine : Path {

        // == MERMBERS ==
        private Point startPoint, endPoint;

        // == GETTER / SETTERS ==
        public double X1 { get { return startPoint.X; } set { startPoint.X = value; } }
        public double X2 { get { return endPoint.X; } set { endPoint.X = value; } }
        public double Y1 { get { return startPoint.Y; } set { startPoint.Y = value; } }
        public double Y2 { get { return endPoint.Y; } set { endPoint.Y = value; } }


        // == CONSTRUCTOR ==
        public BezierLine() { }

        /// <summary>
        /// Constructs and updates a new bezier line.
        /// </summary>
        /// <param name="line"></param>
        public BezierLine(Line line) {
            update(line);
        }


        // == METHODS ==

        /// <summary>
        /// Updates a beizier curve using the points of a given line.
        /// </summary>
        /// <param name="line"></param>
        public void update(Line line) {
            startPoint = new Point(line.X1, line.Y1);
            endPoint = new Point(line.X2, line.Y2);
            update(startPoint, endPoint);
        }

        private Point midpoint(Point one, Point two) {
            return (new Point((one.X + two.X) / 2, (one.Y + two.Y) / 2));
        }

        /// <summary>
        /// Updates bezier curbe using two points.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void update(Point start, Point end) {

            startPoint = start;
            endPoint = end;
            PathFigure pthFigure = new PathFigure();
            pthFigure.StartPoint = startPoint;
            
            // get centerpoint
            double width = Math.Max(X1 - X2, X2 - X1);
            double height = Math.Max(Y1 - Y2, Y2 - Y1);

            Point halfway = midpoint(startPoint,endPoint);
            Debug.WriteLine(halfway.ToString());

            // generate first half of bezier curve
            BezierSegment bzSeg = new BezierSegment();
            bzSeg.Point1 = startPoint;
            bzSeg.Point2 = new Point(startPoint.X, halfway.Y);
            bzSeg.Point3 = halfway;

            // generate first half of bezier curve
            BezierSegment bzSeg2 = new BezierSegment();
            bzSeg2.Point1 = halfway;
            bzSeg2.Point2 = new Point(endPoint.X, halfway.Y);
            bzSeg2.Point3 = endPoint;


            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
            myPathSegmentCollection.Add(bzSeg);
            myPathSegmentCollection.Add(bzSeg2);

            pthFigure.Segments = myPathSegmentCollection;

            PathFigureCollection pthFigureCollection = new PathFigureCollection();
            pthFigureCollection.Add(pthFigure);

            PathGeometry pthGeometry = new PathGeometry();
            pthGeometry.Figures = pthFigureCollection;
            
            this.Stroke = new SolidColorBrush(Windows.UI.Colors.Black);
            this.StrokeThickness = 2;
            this.Data = pthGeometry;
        }
    }
}
