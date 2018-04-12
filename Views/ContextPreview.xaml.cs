using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ContextPreview : UserControl, INotifyPropertyChanged
    {

        private BitmapImage _bitmapImage;

        private BitmapImage BitmapImage
        {
            get => _bitmapImage;
            set
            {
                _bitmapImage = value;
                OnPropertyChanged();
            }
        }

        public DocumentContext Context
        {
            set => BitmapImage = value.GetImage();
        }

        public ContextPreview(DocumentContext context)
        {
            InitializeComponent();
            Context = context;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
