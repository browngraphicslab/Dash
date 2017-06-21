using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Dash;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using Dash.Models;
using System.Diagnostics;

namespace Dash {
    public class TextTemplateModel : TemplateModel {
        public FontWeight FontWeight { get; set; }

        public TextWrapping TextWrapping { get; set; }

        public bool Editable { get; set; }

        public string DefaultText { get; set; }

        public TextTemplateModel(double left, double top, FontWeight weight, TextWrapping wrap = TextWrapping.NoWrap, Visibility visibility = Visibility.Visible, bool editable = false, string defaultText = null)
            : base(left, top, 200, 50, visibility) {
            FontWeight = weight;
            TextWrapping = wrap;
            Editable = editable;
            DefaultText = defaultText;
        }

        /// <summary>
        /// Creates TextBlock using layout information from template and Data 
        /// </summary>
        public override FrameworkElement MakeView(FieldModel fieldModel) {
            if (fieldModel == null && DefaultText == null)
                return null;
            if (fieldModel is ImageFieldModel)
                return new ImageTemplateModel(Left, Top, Width, Height, Visibility).MakeView(fieldModel);
            var textFieldModel = fieldModel as TextFieldModel;

            Binding binding = new Binding {
                Source = fieldModel,
                Path = new PropertyPath("Data")
            };

            var tb = Editable && textFieldModel != null ? (FrameworkElement)new TextBox() : new TextBlock();
            if (tb is TextBox) {
                tb.SetBinding(TextBox.TextProperty, binding);
                (tb as TextBox).TextChanged += ((s, e) => textFieldModel.Data = (s as TextBox).Text);
                (tb as TextBox).FontWeight = FontWeight;
                (tb as TextBox).TextWrapping = TextWrapping;
            } else {
                if (fieldModel == null)
                    (tb as TextBlock).Text = DefaultText;
                else tb.SetBinding(TextBlock.TextProperty, binding);
                (tb as TextBlock).FontWeight = FontWeight;
                (tb as TextBlock).TextWrapping = TextWrapping;
            }

            // make tb move left and right
            var leftBinding = new Binding {
                Source = this,
                Path = new PropertyPath("Left"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(Canvas.LeftProperty, leftBinding);

            // make tb move up and down
            var topBinding = new Binding {
                Source = this,
                Path = new PropertyPath("Top"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(Canvas.TopProperty, topBinding);

            // this if statement allows non-editable text nodes to auto size to
            // their containing bounds
            if (Editable) {
                // make tb width resize
                var widthBinding = new Binding {
                    Source = this,
                    Path = new PropertyPath("Width"),
                    Mode = BindingMode.TwoWay
                };
                tb.SetBinding(FrameworkElement.WidthProperty, widthBinding);

                // make tb height resize
                var heightBinding = new Binding {
                    Source = this,
                    Path = new PropertyPath("Height"),
                    Mode = BindingMode.TwoWay
                };
                tb.SetBinding(FrameworkElement.HeightProperty, heightBinding);
            }

            // make tb appear and disappear
            var visibilityBinding = new Binding {
                Source = this,
                Path = new PropertyPath("Visibility"),
                Mode = BindingMode.TwoWay
            };
            tb.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            return tb;
        }
    }
}