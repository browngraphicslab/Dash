using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class ImageTemplateModel : TemplateModel
    {
        private bool fill;
        public ImageTemplateModel(double left = 0, double top = 0, double width = 0, double height = 0,
            Visibility visibility = Visibility.Visible, bool fill = false)
            : base(left, top, width, height, visibility)
        {
            this.fill = fill;
        }
        
        /// <summary>
        /// Creates Image using layout information from template and Data 
        /// </summary>
        protected override List<FrameworkElement> MakeView(FieldModelController fieldModel, DocumentController context)
        {
            var imageFieldModel = fieldModel is TextFieldModelController ? new ImageFieldModelController(new ImageFieldModel(new Uri((fieldModel as TextFieldModelController).Data))) : fieldModel as ImageFieldModelController;
            Debug.Assert(imageFieldModel != null);
            var image = new Image();
            image.Source = imageFieldModel.Data;

            var translateBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Pos"),
                Mode = BindingMode.TwoWay,
                Converter = new PositionConverter()
            };
            image.SetBinding(UIElement.RenderTransformProperty, translateBinding);

            // make image width resize
            var widthBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Width"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(FrameworkElement.WidthProperty, widthBinding);

            // make image height resize
            var heightBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Height"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(FrameworkElement.HeightProperty, heightBinding);

            // make image appear and disappear
            var visibilityBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Visibility"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;

            if (fill)
                image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
            return new List<FrameworkElement> { image };
        }
    }
}
