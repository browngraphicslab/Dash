
using Windows.UI.Xaml.Controls;
using Windows.UI.Input;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{

    public partial class EditableImage
    {

        public Image Image => xImage;
        PointerPoint p1;
        PointerPoint p2;
       
        public EditableImage()
        {
            InitializeComponent();
        }

        private void Canvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            p1 = e.GetCurrentPoint(xImage);
        }

        private void Canvas_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            p2 = e.GetCurrentPoint(xImage);
        }

        private async void Canvas_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            p2 = e.GetCurrentPoint(xImage);
            xRect.Visibility = Windows.UI.Xaml.Visibility.Visible;
            xRect.Width = (int)Math.Abs(p2.Position.X - p1.Position.X);
            xRect.Height = (int)Math.Abs(p2.Position.Y - p1.Position.Y);
            xRect.SetValue(Canvas.LeftProperty, (p1.Position.X < p2.Position.X) ? p1.Position.X : p2.Position.X);
            xRect.SetValue(Canvas.TopProperty, (p1.Position.Y < p2.Position.Y) ? p1.Position.Y : p2.Position.Y);
            await Task.Delay(100);
            RectangleGeometry geometry = new RectangleGeometry();
            geometry.Rect = new Rect(p1.Position, p2.Position);
            xImage.Clip = geometry;
        }

       
    }
}
