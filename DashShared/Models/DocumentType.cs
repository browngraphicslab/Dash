using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DashShared
{
    public class DocumentType : EntityBase, INotifyPropertyChanged
    {
        // reserved default display for layoutless documents
        public static DocumentType DefaultType = new DocumentType("B492D995-701B-4703-B867-8C957762E352", "Default");

        private string _type;

        public DocumentType()
        {
        }

        public DocumentType(string guid)
        {
            Id = guid;
            _type = guid;
        }

        public DocumentType(string guid, string type)
        {
            Id = guid;
            this._type = type;
        }

        /// <summary>
        ///     The actual name of the type which is displayed
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                if (value == _type) return;
                _type = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Type;
        }
    }
}