﻿using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class PointController : FieldModelController<PointModel>
    {

        public PointController() : this(0, 0)
        {
        }

        public PointController(Point data) : base(new PointModel(data))
        {
            SaveOnServer();

        }

        public PointController(double x, double y) : base(new PointModel(x, y))
        {
            SaveOnServer();

        }

        public PointController(PointModel pointFieldModel) : base(pointFieldModel)
        {

        }

        public override void Init()
        {

        }

        /// <summary>
        ///     The <see cref="Dash.PointModel" /> associated with this <see cref="PointController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PointModel PointFieldModel => Model as PointModel;

        public override FieldControllerBase GetDefaultController()
        {
            return new PointController(0, 0);
        }

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool TrySetValue(object value)
        {
            if (value is Point)
            {
                Data = (Point)value;
                return true;
            }
            return false;
        }
        public Point Data
        {
            get { return PointFieldModel.Data; }
            set
            {
                if(PointFieldModel.Data != value)
                {
                    PointFieldModel.Data = value;
                    OnFieldModelUpdated(null);
                }
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Point;

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public override string ToString()
        {
            return $"({Data})";
        }

        public override FieldControllerBase Copy()
        {
            return new PointController(Data);
        }
    }
}
