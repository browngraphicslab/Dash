using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// Settings pane that shows up in interfacebuilder when gridview is selected (as composite view) 
    /// </summary>
    public sealed partial class GridViewSettings : UserControl
    {
        public GridViewSettings()
        {
            this.InitializeComponent();
        }

        public GridViewSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType == GridViewLayout.DocumentType);

            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            xAlignmentRow.Children.Add(new AlignmentSettings(docController, context));
            BindSpacing(docController, context, xSpacingSliderTextBox, xSpacingSlider);
        }

        /// <summary>
        /// Bind the value of spacingSlider to the spacing between the items in gridview  
        /// </summary>
        public static void BindSpacing(DocumentController docController, Context context, TextBox tb, Slider slider)
        {
            var spacingController =
                    docController.GetDereferencedField(GridViewLayout.SpacingKey, context) as NumberFieldModelController;
            Debug.Assert(spacingController != null);

            var spacingBinding = new Binding()
            {
                Source = spacingController,
                Path = new PropertyPath(nameof(spacingController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            tb.SetBinding(TextBox.TextProperty, spacingBinding);

            var spacingTextBinding = new Binding()
            {
                Source = tb,
                Path = new PropertyPath(nameof(tb.Text)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            slider.SetBinding(Slider.ValueProperty, spacingTextBinding);
        }
    }
}
