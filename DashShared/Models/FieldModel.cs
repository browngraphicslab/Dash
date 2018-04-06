using System;

namespace DashShared
{ 

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FieldModelTypeAttribute : Attribute
    {
        private TypeInfo _name;
        public double version;

        public FieldModelTypeAttribute(TypeInfo name)
        {
            this._name = name;

            // Default value.
            version = 1.0;
        }

        public TypeInfo GetFieldType()
        {
            return _name;
        }
    }

    public abstract class FieldModel : EntityBase
    {
        protected FieldModel(string id = null) : base(id)
        {
            // Initialize Local Variables

            // Add Any Events
        }
    }
}
