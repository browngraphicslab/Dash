using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;

namespace Dash
{
    class DraggableEllipse : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Ellipse _e;

        public DraggableEllipse(double X, double Y)
        {
            _e = new Ellipse
            {
                Width = 10, Height = 0, Fill = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(_e, X);
            Canvas.SetTop(_e, Y);
        }
    }
}
