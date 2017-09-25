using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class RectFieldModelController : FieldModelController<RectFieldModel>
    {
        public RectFieldModelController(Rect data) :base(new RectFieldModel(data)) { }
        public RectFieldModelController(double x, double y, double width, double height) : base(new RectFieldModel(x, y, width, height)) { }

        private RectFieldModelController(RectFieldModel rectFieldModel) : base(rectFieldModel)
        {

        }

        public static RectFieldModelController CreateFromServer(RectFieldModel rectFieldModel)
        {
            return ContentController<FieldModel>.GetController<RectFieldModelController>(rectFieldModel.Id) ??
                    new RectFieldModelController(rectFieldModel);
        }



        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public RectFieldModel RectFieldModel => Model as RectFieldModel;

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
            return new RectFieldModelController(0, 0, 1, 1);
        }

        public override object GetValue(Context context)
        {
            return Data;
        }
        public override bool SetValue(object value)
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
            get { return RectFieldModel.Data; }
            set
            {
                if (RectFieldModel.Data != value)
                {
                    RectFieldModel.Data = value;
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(Model, dto =>
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

        public override FieldModelController<RectFieldModel> Copy()
        {
            return new RectFieldModelController(Data);
        }
    }
}
