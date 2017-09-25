using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared.Models
{
    public enum FieldTypeEnum
    {
        Point,
        DocumentCollection,
        Operator,
        List,
        Document,
        Ink,
        Number,
        Reference,
        Rect,
        Text,
        RichText
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FieldModelType : Attribute
    {
        private FieldTypeEnum _name;
        public double version;

        public FieldModelType(FieldTypeEnum name)
        {
            this._name = name;

            // Default value.
            version = 1.0;
        }

        public FieldTypeEnum GetType()
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
