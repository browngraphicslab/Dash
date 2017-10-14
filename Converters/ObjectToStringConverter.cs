using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dash.Converters
{
    public class ObjectToStringConverter : SafeDataToXamlConverter<object, string>
    {
        private Context _context;

        public ObjectToStringConverter(Context context)
        {
            _context = context;
        }

        public override string ConvertDataToXaml(object refField, object parameter = null)
        {
            var fieldData = (refField as ReferenceFieldModelController)?.DereferenceToRoot(_context)?.GetValue(_context) ?? refField;

            if (fieldData is DocumentController)
            {
                return new DocumentControllerToStringConverter(_context).ConvertDataToXaml(fieldData as DocumentController);
            }
            if (fieldData is List<DocumentController>)
            {
                return new DocumentCollectionToStringConverter(_context).ConvertDataToXaml(fieldData as List<DocumentController>);
            }
            return fieldData == null ? "<null>" : fieldData.ToString();
         }

        public override object ConvertXamlToData(string xaml, object parameter = null)
        {
            return xaml;
        }
    }
}
