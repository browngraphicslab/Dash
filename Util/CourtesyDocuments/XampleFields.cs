using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    class XampleFields : CourtesyDocument
    {
        public static DocumentType XampleFieldsType =
            new DocumentType("7A4EE83B-785F-4DEE-BCBE-3AE8187C69E6", "Xample 50 Fields");

        private static Random r = new Random();

        public XampleFields(int numFields, TypeInfo fieldType)
        {
            // create a document with two images
            var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);

            if (fieldType == TypeInfo.Text)
            {
                for (int i = 0; i < numFields; ++i)
                {
                    KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid("Text " + i), "Text " + i);
                    fields[key] = new TextFieldModelController("This is example text " + i);
                }
            }
            else if (fieldType == TypeInfo.Text)
            {
                for (int i = 0; i < numFields; ++i)
                {
                    KeyController key = new KeyController(DashShared.Util.GetDeterministicGuid("Number " + i), "Number " + i);
                    fields[key] = new NumberFieldModelController(r.NextDouble() * 100);
                }
            } else throw new ArgumentException();

            Document = new DocumentController(fields, XampleFieldsType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }
    }
}
