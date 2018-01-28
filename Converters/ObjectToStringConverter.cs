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
        private readonly Context _context;

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

            // convert references to a string representation
            var fieldData = (refField as ReferenceController)?.DereferenceToRoot(_context)?.GetValue(_context);
            if (fieldData == null && !(refField is ReferenceController))
            {
                fieldData = (refField as FieldControllerBase)?.GetValue(_context);
            }

            // convert ListControllers to a string representation
            var ilist = fieldData as IList;
            if (ilist != null)
            {
                if (ilist.Count == 0)
                    return "<empty list>";
                return "[" + string.Join(", ", ilist.Cast<object>().Select(o => o.ToString())) + "]";
            }

            // use null as a fallback value if we have nothing better
            return fieldData == null ? "<null>" : fieldData.ToString();
         }

        public override object ConvertXamlToData(string xaml, object parameter = null)
        {
            return xaml;
        }
    }
}
