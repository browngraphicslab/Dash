using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Text;
using Dash.Annotations;
using FontWeights = Windows.UI.Text.FontWeights;

namespace Dash
{
    public class PresentationNumberViewModel : INotifyPropertyChanged
    {
        private FontWeight _fontWeight;
        public int Num { get; }

        public FontWeight FontWeight
        {
            get => _fontWeight;
            set
            {
                _fontWeight = value;
                OnPropertyChanged();
            }
        }

        public PresentationNumberViewModel(int num)
        {
            FontWeight = FontWeights.Normal;
            Num = num;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}