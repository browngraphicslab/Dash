using System;

namespace DashShared
{
    [FieldModelType(TypeInfo.Key)]
    public class KeyModel : FieldModel
    {
        /// <summary>
        /// The name of the entity, this is useful for search and provides a front end that
        /// can be displayed to the user
        /// </summary>
        public string Name { get; set; }

        public KeyModel(string name, string guid) : base(guid) {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyModel k))
            {
                return false;
            }
            return k.Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name; 
        }
    }
}
