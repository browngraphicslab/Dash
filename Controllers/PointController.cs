using Windows.Foundation;
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

        }

        public PointController(double x, double y) : base(new PointModel(x, y))
        {

        }

        public PointController(PointModel pointFieldModel) : base(pointFieldModel)
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
                if (PointFieldModel.Data != value)
                {
                    SetData(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetData(Point val, bool withUndo = true)
        {
            Point data = PointFieldModel.Data;
            UndoCommand newEvent = new UndoCommand(() => SetData(val, false), () => SetData(data, false));

            PointFieldModel.Data = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public override TypeInfo TypeInfo => TypeInfo.Point;

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<PointOperator>() + $"({Data.X}, {Data.Y})";
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
