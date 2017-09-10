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
        public PointFieldModelController(Point data) :base(new PointFieldModel(data), false) { }
        public PointFieldModelController(double x, double y) : base(new PointFieldModel(x, y), false) { }

        private PointFieldModelController(PointFieldModel pointFieldModel) : base(pointFieldModel, true)
        {

        }

        public static PointFieldModelController CreateFromServer(PointFieldModel pointFieldModel)
        {
            return ContentController.GetController<PointFieldModelController>(pointFieldModel.Id) ??
                    new PointFieldModelController(pointFieldModel);
        }

        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PointFieldModel PointFieldModel => FieldModel as PointFieldModel;

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

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
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
                if (SetProperty(ref PointFieldModel.Data, value))
                {
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(FieldModel, dto =>
                    {

                    }, exception =>
                    {

                    });
                    OnFieldModelUpdated(null);
                    // update local
                    // update server
                }
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
