using System.Collections.Generic;
using System.Linq;
using Dash.Models.DragModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InfoDot : UserControl
    {
        private DocumentView dotDocView;
        public InfoDot(DocumentView dcon)
        {
            dotDocView = dcon;
            this.InitializeComponent();
        }

        private void OperatorEllipse_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var selected = (dotDocView.GetFirstAncestorOfType<CollectionFreeformView>())?.SelectedDocs.Select((dv) => dv.ViewModel.DocumentController);
            if (selected?.Count() > 0)
            {
                args.Data.Properties[nameof(List<DragDocumentModel>)] =
                    new List<DragDocumentModel>(selected.Select((s) => new DragDocumentModel(s, true)));
            }
            else
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(dotDocView.ViewModel.DocumentController, false);
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

        private void XOperatorEllipseBorder_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            MainPage.Instance.AddInfoDot(dotDocView, this);
        }

        private void XOperatorEllipseBorder_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            OperatorEllipse.Visibility = Visibility.Collapsed;
        }

        private void XOperatorEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.ManipulationMode = ManipulationModes.None;
            e.Handled = !e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
        }

        private void OperatorEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ManipulationMode = ManipulationModes.All;
        }
    }
}
