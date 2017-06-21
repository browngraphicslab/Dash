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
        protected override List<UIElement> MakeView(FieldModel fieldModel)
        {
            var imageFieldModel = fieldModel is TextFieldModel ? new ImageFieldModel(new Uri((fieldModel as TextFieldModel).Data)) :  fieldModel as ImageFieldModel;
            Debug.Assert(imageFieldModel != null);
            var image = new Image();
            image.Source = imageFieldModel.Data;

            // make image move left and right
            var leftBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Left"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Canvas.LeftProperty, leftBinding);

            // make image move up and down
            var topBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Top"),
                Mode = BindingMode.TwoWay
            };
            image.SetBinding(Canvas.TopProperty, topBinding);

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
            
            if (fill)
                image.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
            return new List<UIElement>(new UIElement[] { image });
        }
    }
}
