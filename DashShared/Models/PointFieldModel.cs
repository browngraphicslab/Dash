using System;
using DashShared;

namespace DashShared
{
    public class PointFieldModel : FieldModel
    {
        // server-side point wrapping class
        public class Point {
            private double x, y;
            public Point()
            {
                this.x = 0;
                this.y = 0;
            }
            public Point(double x, double y) {
                this.x = x;
                this.y = y;
            }
            public double X { get { return this.x; } set { this.x = value; } }
            public double Y { get { return this.y; } set { this.y = value; } }
        }

        public PointFieldModel() { }

        public PointFieldModel(Point data) : this()
        {
            Data = data;
        }

        public PointFieldModel(double x, double y) : this(new Point(x, y))
        {
        }

        public Point Data;

        public override string ToString()
        {
            return $"PointFieldModel: {Data}";
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Point, Data);
        }
    }
}
