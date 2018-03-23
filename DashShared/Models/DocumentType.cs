using System;
using System.Collections.Generic;
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


        /// <summary>
        /// If you get an exception here, you are trying to compare 2 types with ==.
        /// This causes problems with data persistence so you should always use .Equals to compare types
        /// </summary>
        public static bool operator ==(DocumentType t1, DocumentType t2)
        {
            if (ReferenceEquals(t1, null))
            {
                return ReferenceEquals(t2, null);
            }
            if (ReferenceEquals(t2, null))
            {
                return false;
            }
            throw new NotImplementedException();
        }
        
        public static bool operator !=(DocumentType t1, DocumentType t2)
        {
            return !(t1 == t2);
        }

        protected bool Equals(DocumentType other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentType) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}