using System.Diagnostics;
using Windows.Foundation;

namespace Dash
{
    public class PointFieldModelController : FieldModelController
    {
        public PointFieldModelController(Point data) :base(new PointFieldModel(data)) { }
        public PointFieldModelController(double x, double y) : base(new PointFieldModel(x, y)) { }

        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PointFieldModel PointFieldModel => FieldModel as PointFieldModel;

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Debug.Assert(fieldModel is PointFieldModelController);
            Data = ((PointFieldModelController) fieldModel).Data;
        }

        public Point Data
        {
            get { return PointFieldModel.Data; }
            set
            {
                if (SetProperty(ref PointFieldModel.Data, value))
                {
                    // update local
                    // update server
                }
                FireFieldModelUpdated();
            }
        }
    }
}
