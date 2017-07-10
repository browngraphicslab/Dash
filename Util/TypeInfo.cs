using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    [Flags]
    public enum TypeInfo
    {
        None = 0x0,
        Number = 0x1,
        Text = 0x2,
        Image = 0x4,
        Collection = 0x8,
        Document = 0x10,
        Reference = 0x20,
        Operator = 0x40,
        Point = 0x80,
        List = 0x100
    }

    public class TypeInfoHelper
    {
        public static FieldModelController CreateFieldModelController(TypeInfo t)
        {
            switch (t)
            {
                case TypeInfo.Text:
                    return new TextFieldModelController("");
                case TypeInfo.Number:
                    return new NumberFieldModelController();
                case TypeInfo.Image:
                    return new ImageFieldModelController();
                case TypeInfo.Collection:
                    return new DocumentCollectionFieldModelController(new List<DocumentController>());
                case TypeInfo.Document:
                    return new DocumentFieldModelController(null);
                case TypeInfo.Reference:
                    return new ReferenceFieldModelController("", null);
                case TypeInfo.Operator://TODO What should this do?
                    return null;
                case TypeInfo.Point:
                    return new PointFieldModelController(0, 0);
                default:
                    return null;
            }
        }
    }
}
