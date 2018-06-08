using System;
using System.Collections.Generic;
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
using Syncfusion.UI.Xaml.Controls.Media;
using System.Diagnostics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GroupingSubtoolbar : UserControl, ICommandBarBased
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(GroupingSubtoolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private DocumentView currentDocView;
        private DocumentController currentDocController;

        public GroupingSubtoolbar()
        {
            this.InitializeComponent();
            FormatDropdownMenu();

            xGroupCommandbar.Loaded += delegate
            {
                var sp = xGroupCommandbar.GetFirstDescendantOfType<StackPanel>();
                sp.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;
            };
        }

        private void FormatDropdownMenu()
        {
            xShapeOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
            xShapeOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
            xShapeOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        private void GroupForegroundColorPicker_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is SfColorPicker colorPicker)
            {
                
                currentDocView.ViewModel.BackgroundBrush = new SolidColorBrush(colorPicker.SelectedColor);
                //if (currentDocController != null)
                //{
                //    var color = colorPicker.SelectedColor;
                //    currentDocController.SetField<TextController>(KeyStore.BackgroundColorKey, color, true);
                //}
            }
        }

        public ComboBox GetComboBox()
        {
            return xShapeOptionsDropdown;
        }

        public void CommandBarOpen(bool status)
        {
            xGroupCommandbar.IsOpen = status;
            xGroupCommandbar.IsEnabled = true;
            xGroupCommandbar.Visibility = Visibility.Visible;
            xShapeOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        internal void SetGroupBinding(DocumentView selection)
        {
            currentDocView = selection;
            currentDocController = currentDocView.ViewModel.DocumentController;
        }

        private void ShapeOptionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentDocController != null)
            {
                switch (xShapeOptionsDropdown.SelectedIndex)
                {
                    case 0:
                        currentDocController.SetField<TextController>(KeyStore.AdornmentShapeKey, BackgroundShape.AdornmentShape.Rectangular.ToString(), true);
                        break;
                    case 1:
                        currentDocController.SetField<TextController>(KeyStore.AdornmentShapeKey, BackgroundShape.AdornmentShape.Elliptical.ToString(), true);
                        break;
                    case 2:
                        currentDocController.SetField<TextController>(KeyStore.AdornmentShapeKey, BackgroundShape.AdornmentShape.Rounded.ToString(), true);
                        break;
                    case 3:
                        //Arbitrary polygon: collect user input points with a nice UI.
                        break;
                    default:
                        currentDocController.SetField<TextController>(KeyStore.AdornmentShapeKey, BackgroundShape.AdornmentShape.Rectangular.ToString(), true);
                        break;
                }
            }
        }

        private void XGroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xGroupCommandbar.IsOpen = true;
            xGroupCommandbar.IsEnabled = true;
        }

        private void XUngroup_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xGroupCommandbar.IsOpen = true;
            xGroupCommandbar.IsEnabled = true;
        }
    }
}
