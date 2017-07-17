using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    public class DocumentType : EntityBase, INotifyPropertyChanged
    {
        // reserved default display for layoutless documents
        public static DocumentType DefaultType = new DocumentType("B492D995-701B-4703-B867-8C957762E352","Default");

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string type;

        /// <summary>
        /// The actual name of the type which is displayed 
        /// </summary>
        public string Type { get {
                return type;
            } set {
                if (value != this.type) {
                    this.type = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DocumentType()
        {
        }

        public DocumentType(string guid)
        {
            Id = guid;
        }

        public DocumentType(string guid, string type)
        {
            Id = guid;
            this.type = type;
        }

        public override string ToString()
        {
            return Type;
        }
    }
}
