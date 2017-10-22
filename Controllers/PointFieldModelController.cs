using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class PointFieldModelController : FieldModelController<PointFieldModel>
    {
        public PointFieldModelController(Point data) :base(new PointFieldModel(data) ) { }
        public PointFieldModelController(double x, double y) : base(new PointFieldModel(x, y)) { }

        public PointFieldModelController(PointFieldModel pointFieldModel) : base(pointFieldModel)
        {

        }

        public override void Init()
        {

        }

        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public PointFieldModel PointFieldModel => Model as PointFieldModel;

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

        public override FieldControllerBase GetDefaultController()
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
                if(PointFieldModel.Data != value)
                {
                    PointFieldModel.Data = value;
                    // Update the server
                    UpdateOnServer();
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

        public override FieldModelController<PointFieldModel> Copy()
        {
            return new PointFieldModelController(Data);
        }
    }
}
