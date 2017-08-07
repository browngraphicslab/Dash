using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dash.Converters
{
    public class BoundReferenceToStringConverter : SafeDataToXamlConverter<ReferenceFieldModelController, string>
    {
        private Context _context;

        public BoundReferenceToStringConverter(Context context)
        {
            _context = context;
        }

        public override string ConvertDataToXaml(ReferenceFieldModelController data, object parameter = null)
        {
            var field = data.DereferenceToRoot(_context);
            if (field is DocumentFieldModelController)
                return new DocumentControllerToStringConverter(_context).ConvertDataToXaml((field as DocumentFieldModelController).Data);
            if (field is DocumentCollectionFieldModelController)
                return new DocumentCollectionToStringConverter(_context).ConvertDataToXaml((field as DocumentCollectionFieldModelController).Data);
            if (field is NumberFieldModelController)
                return new StringToDoubleConverter(0).ConvertDataToXaml((field as NumberFieldModelController).Data);
            if (field is TextFieldModelController)
                return (field as TextFieldModelController).Data;
            return "bound field reference string -- need to call appropriate converters here";
        }

        public override ReferenceFieldModelController ConvertXamlToData(string xaml, object parameter = null)
        {
            return null;
        }
    }
}
