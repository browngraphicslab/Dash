using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    public class PointFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new <see cref="PointFieldModelController"/> associated with the passed in <see cref="Dash.PointFieldModel" />
        /// </summary>
        /// <param name="pointFieldModel">The model which this controller will be operating over</param>
        public PointFieldModelController(PointFieldModel pointFieldModel) : base(pointFieldModel)
        {
            PointFieldModel = pointFieldModel;
        }

        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PointFieldModel PointFieldModel { get; }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Debug.Assert(fieldModel is PointFieldModelController);
            Data = (fieldModel as PointFieldModelController).Data;
        }

        public override FrameworkElement GetTableCellView()
        {
            var textBlockText = PointFieldModel.Data.ToString();
            return GetTableCellViewOfScrollableText(textBlockText);
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
