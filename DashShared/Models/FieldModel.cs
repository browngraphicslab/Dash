using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared.Models
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

        public TypeInfo GetType()
        {
            return _name;
        }
    }

    public abstract class FieldModel : EntityBase
    {
        public FieldModel(string id = null) : base(id)
        {
            // Initialize Local Variables

            // Add Any Events
        }
    }
}
