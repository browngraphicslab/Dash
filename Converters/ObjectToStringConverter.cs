using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Converters;
using System.Collections;

namespace Dash
{
    public class ObjectToStringConverter : SafeDataToXamlConverter<object, string>
    {
        private Context _context;

        public ObjectToStringConverter(Context context)
        {
            _context = context;
        }
        public ObjectToStringConverter()
        {
            _context = null;
        }

        public override string ConvertDataToXaml(object refField, object parameter = null)
        {
            var fieldData = (refField as ReferenceController)?.DereferenceToRoot(_context)?.GetValue(_context) ?? refField;

            if (fieldData is DocumentController)
            {
                return new DocumentControllerToStringConverter(_context).ConvertDataToXaml(fieldData as DocumentController);
            }
            if (fieldData is List<DocumentController>)
            {
                return new DocumentCollectionToStringConverter(_context).ConvertDataToXaml(fieldData as List<DocumentController>);
            }

            var ilist = fieldData as IList;
            if (ilist != null)
            {
                if (ilist.Count == 0)
                    return "<empty list>";
                var item1 = ilist[0];
                object val = item1;
                if (item1 is FieldControllerBase)
                {
                    val = (item1 as FieldControllerBase).GetValue(null);
                }
                return val.GetType().ToString().Replace("System.", "") + "[" + (fieldData as IList).Count + "]";
            }

            return fieldData == null || fieldData is ReferenceController ? "<null>" : fieldData.ToString();
         }

        public override object ConvertXamlToData(string xaml, object parameter = null)
        {
            return xaml;
        }
    }
}
