using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class RectController : FieldModelController<RectModel>
    {
        public RectController(Rect data) : base(new RectModel(data))
        {

        }

        public RectController(double x, double y, double width, double height) : base(
            new RectModel(x, y, width, height))
        {

        }

        public RectController(RectModel rectModel) : base(rectModel)
        {

        }

        /// <summary>
        ///     The <see cref="Dash.PointModel" /> associated with this <see cref="PointController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public RectModel RectModel => Model as RectModel;

        public override FieldControllerBase GetDefaultController()
        {
            return new RectController(0, 0, 1, 1);
        }

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool TrySetValue(object value)
        {
            if (value is Rect)
            {
                Data = (Rect)value;
                return true;
            }
            return false;
        }
        public Rect Data
        {
            get => RectModel.Data;
            set
            {
                if (RectModel.Data != value)
                {
                    Rect data = RectModel.Data;
                    UndoCommand newEvent = new UndoCommand(() => Data = value, () => Data = data);

                    RectModel.Data = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public override TypeInfo TypeInfo => TypeInfo.Point;

        public override string ToString()
        {
            return $"({Data})";
        }

        public override FieldControllerBase Copy()
        {
            return new RectController(Data);
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "RectController";
        }
    }
}
