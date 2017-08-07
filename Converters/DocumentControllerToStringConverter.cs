using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class DocumentCollectionToStringConverter : SafeDataToXamlConverter<List<DocumentController>, string>
    {
        
        private Context _context;

        public DocumentCollectionToStringConverter()
        {
        }

        public DocumentCollectionToStringConverter(Context context)
        {
            _context = context;
        }

        string GetPrimaryKeyString(DocumentController data)
        {
            var keyList = data.GetDereferencedField(KeyStore.PrimaryKeyKey, _context);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = data.GetDereferencedField(new KeyController((k as TextFieldModelController).Data), _context);
                    if (keyField is TextFieldModelController)
                        docString += (keyField as TextFieldModelController).Data + " ";
                    else if (keyField is DocumentFieldModelController)
                    {
                        docString += GetPrimaryKeyString((keyField as DocumentFieldModelController).Data);
                    }
                }
                return docString.TrimEnd(' ') + ">";
            }
            return "";
        }

        public override string ConvertDataToXaml(List<DocumentController> dataList, object parameter = null)
        {
            var docListString = "{";
            foreach (var data in dataList)
            {
                docListString += GetPrimaryKeyString(data) + " ";
            }
            docListString = docListString.Trim(' ');
            docListString += "}";
            return docListString;
        }

        public override List<DocumentController> ConvertXamlToData(string xaml, object parameter = null)
        {
            var docList = new List<DocumentController>();
            var docs = xaml.Trim('{','}').Split('>');
            foreach (var d in docs)
            {
                var doc = new DocumentControllerToStringConverter(_context).ConvertXamlToData(d + '>');
                if (doc != null)
                    docList.Add(doc);
            }
            return docList;
        }
    }

    public class DocumentControllerToStringConverter : SafeDataToXamlConverter<DocumentController, string>
    {
        private DocumentController _doc;
        private Context _context;

        public DocumentControllerToStringConverter()
        {
        }

        public DocumentControllerToStringConverter(Context context)
        {
            _context = context;
        }

        public override string ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            _doc = data;
            var keyList = data.GetDereferencedField(KeyStore.PrimaryKeyKey, _context);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = data.GetDereferencedField(new KeyController((k as TextFieldModelController).Data), _context);
                    if (keyField is TextFieldModelController)
                        docString += (keyField as TextFieldModelController).Data + " ";
                }
                return docString.TrimEnd(' ') + ">";
            }
            return data.GetId();
        }

        public override DocumentController ConvertXamlToData(string xaml, object parameter = null)
        {
            var values = xaml.TrimStart('<').TrimEnd('>').Split(' ');
            var keyList = _doc?.GetDereferencedField(KeyStore.PrimaryKeyKey, _context);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                foreach (var dmc in ContentController.GetControllers<DocumentController>())
                    if (!dmc.DocumentType.Type.Contains("Box") && !dmc.DocumentType.Type.Contains("Layout"))
                    {
                        bool found = true;
                        foreach (var k in keys.Data)
                        {
                            var key = new KeyController((k as TextFieldModelController).Data);
                            var index = keys.Data.IndexOf(k);
                            var derefValue = (dmc.GetDereferencedField(key, _context) as TextFieldModelController)?.Data;
                            if (derefValue != null)
                            {
                                if (values[index] != derefValue)
                                {
                                    found = false;
                                    break;
                                }
                            }
                            else
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            _doc = dmc;
                            return dmc;
                        }
                    }
            }
            var doc = DocumentController.FindDocMatchingPrimaryKeys(values);
            if (doc != null)
            {
                _doc = doc;
                return _doc;
            }
            return DBTest.DBNull;
        }
    }
    public class DocumentFieldModelToStringConverter : SafeDataToXamlConverter<DocumentFieldModelController, string>
    {
        public DocumentFieldModelToStringConverter()
        {
        }

        public override string ConvertDataToXaml(DocumentFieldModelController data, object parameter = null)
        {
            return new DocumentControllerToStringConverter().ConvertDataToXaml(data.Data);
        }

        public override DocumentFieldModelController ConvertXamlToData(string xaml, object parameter = null)
        {
            return new DocumentFieldModelController(new DocumentControllerToStringConverter().ConvertXamlToData(xaml));
        }
    }
}
