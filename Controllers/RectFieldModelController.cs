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

namespace Dash
{
    public class RectFieldModelController : FieldModelController
    {
        public RectFieldModelController(Rect data) :base(new RectFieldModel(data)) { }
        public RectFieldModelController(double x, double y, double width, double height) : base(new RectFieldModel(x, y, width, height)) { }

        /// <summary>
        ///     The <see cref="Dash.PointFieldModel" /> associated with this <see cref="PointFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public RectFieldModel RectFieldModel => FieldModel as RectFieldModel;

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            Debug.Assert(fieldModel is RectFieldModelController);
            Data = ((RectFieldModelController)fieldModel).Data;
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
            return new RectFieldModelController(0, 0, 1, 1);
        }

        public Rect Data
        {
            get { return RectFieldModel.Data; }
            set
            {
                if (SetProperty(ref RectFieldModel.Data, value))
                {
                    // Update the server
                    RESTClient.Instance.Fields.UpdateField(FieldModel, dto =>
                    {

                    }, exception =>
                    {

                    });
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
            return new RectFieldModelController(Data);
        }
    }
}
