using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class PointController : FieldModelController<PointModel>
    {
        public PointController(Point data) :base(new PointModel(data) ) { }
        public PointController(double x, double y) : base(new PointModel(x, y)) { }

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
            return new PointController(0, 0);
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

        public override FieldModelController<PointModel> Copy()
        {
            return new PointController(Data);
        }
    }
}
