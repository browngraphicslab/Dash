using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class DocumentControllerToStringConverter : SafeDataToXamlConverter<DocumentController, string>
    {
        private DocumentController _doc;

        public DocumentControllerToStringConverter(DocumentController doc)
        {
            _doc = doc;
        }

        public override string ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            _doc = data;
            var keyList = _doc.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = _doc.GetDereferencedField(new Key((k as TextFieldModelController).Data), null);
                    if (keyField is TextFieldModelController)
                        docString += (keyField as TextFieldModelController).Data + " ";
                }
                return docString.TrimEnd(' ') + ">";
            }
            return _doc.GetId();
        }

        public override DocumentController ConvertXamlToData(string xaml, object parameter = null)
        {
            var values = xaml.TrimStart('<').TrimEnd('>').Split(' ');
            var keyList = _doc.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                int count = 0;
                foreach (var doc in ContentController.GetControllers<DocumentController>())
                {
                    count++;
                    bool found = true;
                    foreach (var k in keys.Data)
                    {
                        var key = new Key((k as TextFieldModelController).Data);
                        var index = keys.Data.IndexOf(k);
                        var derefValue = (doc.GetDereferencedField(key, null) as TextFieldModelController)?.Data;
                        if (derefValue != null)
                        {
                            if (values[index] != derefValue)
                            {
                                found = false;
                                break;
                            }
                        } else
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                    {
                        _doc = doc;
                        return doc;
                    }
                }
            }
            return _doc;
        }
    }
}
