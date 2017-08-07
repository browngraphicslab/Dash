using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;

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

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewOfScrollableText(BindTextOrSetOnce);
        }

        private void BindTextOrSetOnce(TextBlock textBlock)
        {
            var textBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Data)),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);
        }

        public override FieldModelController GetDefaultController()
        {
            return new PointFieldModelController(0, 0);
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
                OnFieldModelUpdated(null);
            }
        }
        public override TypeInfo TypeInfo => TypeInfo.Point;

        public override string ToString()
        {
            return $"({Data})";
        }

        public override FieldModelController Copy()
        {
            return new PointFieldModelController(Data);
        }
    }
}
