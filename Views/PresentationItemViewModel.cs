using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Text;
using Dash.Annotations;
using FontWeights = Windows.UI.Text.FontWeights;

namespace Dash
{
    public class PresentationItemViewModel : INotifyPropertyChanged
    {
        private int _num;
        public int Num
        {
            get => _num;
            set
            {
                if (value == _num) return;
                _num = value;
                OnPropertyChanged();
            }
        }

        public DocumentController Document { get; }
        public DocumentController Parent => Document.GetRegionDefinition() ?? Document;

        private FontWeight _fontWeight;
        public FontWeight FontWeight
        {
            get => _fontWeight;
            set
            {
                _fontWeight = value;
                OnPropertyChanged();
            }
        }

        public PresentationItemViewModel(DocumentController doc, int num)
        {
            Document = doc;
            _fontWeight = FontWeights.Normal;
            _num = num;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
