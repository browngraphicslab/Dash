using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InfoDot : UserControl
    {
        public InfoDot()
        {
            this.InitializeComponent();
        }

        private void OperatorEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
           // args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(ViewModel.DocumentController, false);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void OperatorEllipse_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Gold);
                ellipse.Height += 3;
                ellipse.Width += 3;
            }
        }

        private void OperatorEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ellipse.Fill = (SolidColorBrush)App.Instance.Resources["FieldHandleColor"];
                ellipse.Height -= 3;
                ellipse.Width -= 3;
            }
        }
    }
}
