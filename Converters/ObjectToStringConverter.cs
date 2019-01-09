using System.Linq;
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

        public bool DereferenceData { get; set; } = true;

        public override string ConvertDataToXaml(object refField, object parameter = null)
        {

            // convert references to a string representation
            var fieldData = DereferenceData ? (refField as ReferenceController)?.DereferenceToRoot(_context)?.GetValue() : refField as ReferenceController;
            if (fieldData == null && !(refField is ReferenceController))
            {
                fieldData = refField;
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
