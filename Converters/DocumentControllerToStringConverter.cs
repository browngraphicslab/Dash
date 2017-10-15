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
        private bool _returnCount = false;

        public DocumentCollectionToStringConverter(bool returnCount=false)
        {
            _returnCount = returnCount;
        }

        public DocumentCollectionToStringConverter(Context context, bool returnCount=false)
        {
            _context = context;
            _returnCount = returnCount;
        }

        string GetPrimaryKeyString(DocumentController data)
        {
            var keys = data.GetDereferencedField< ListFieldModelController<TextFieldModelController>>(KeyStore.PrimaryKeyKey, _context);
            var context = _context;
            if (keys == null)
            {
                var docContext = data.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, new Context(data))?.Data;
                if (docContext != null)
                {
                    context = new Context(docContext);
                    keys = docContext.GetDereferencedField< ListFieldModelController<TextFieldModelController>>(KeyStore.PrimaryKeyKey, context);
                    data = docContext;
                }
            }
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = data.GetDereferencedField(new KeyController((k as TextFieldModelController).Data), context);
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
            if (_returnCount)
                return dataList.Count().ToString();
            var docListString = "{";
            foreach (var data in dataList)
            {
                docListString += GetPrimaryKeyString(data) + ",";
            }
            docListString = docListString.Trim(',');
            docListString += "}";
            return docListString;
        }

        public override List<DocumentController> ConvertXamlToData(string xaml, object parameter = null)
        {
            var docList = new List<DocumentController>();
            var docs = xaml.Trim('{','}').Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var d in docs)
            {
                var doc = new DocumentControllerToStringConverter(_context).ConvertXamlToData(d.TrimStart(',', ' ') + '>');
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
    public class DocumentViewModelToStringConverter : SafeDataToXamlConverter<DocumentViewModel, string>
    {
        private DocumentViewModel _vm;

        public DocumentViewModelToStringConverter()
        {
        }

        public DocumentViewModelToStringConverter(DocumentViewModel vm)
        {
            _vm = vm;
        }

        public override string ConvertDataToXaml(DocumentViewModel data, object parameter = null)
        {
            _vm = data;
            var keyList = data.DocumentController.GetDereferencedField(KeyStore.PrimaryKeyKey, data.Context);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "";
                foreach (var k in keys.Data)
                {
                    var keyField = data.DocumentController.GetDereferencedField(new KeyController((k as TextFieldModelController).Data), data.Context);
                    if (keyField is TextFieldModelController)
                        docString += (keyField as TextFieldModelController).Data + " ";
                }
                return docString.TrimEnd(' ');
            }
            return data.DocumentController.GetId();
        }

        public override DocumentViewModel ConvertXamlToData(string xaml, object parameter = null)
        {
            var values = xaml.Split(' ');
            var keyList = _vm.DocumentController?.GetDereferencedField(KeyStore.PrimaryKeyKey, _vm.Context);
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
                            var derefValue = (dmc.GetDereferencedField(key, _vm.Context) as TextFieldModelController)?.Data;
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
                            _vm.DocumentController = dmc;
                            return _vm;
                        }
                    }
            }
            var doc = DocumentController.FindDocMatchingPrimaryKeys(values);
            if (doc != null)
            {
                _vm.DocumentController = doc;
                return _vm;
            }
            return null;
        }
    }
    public class DocumentToViewModelConverter : SafeDataToXamlConverter<DocumentController, DocumentViewModel>
    {
        public DocumentToViewModelConverter()
        {
        }
        

        public override DocumentViewModel ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            return new DocumentViewModel(data);
        }

        public override DocumentController ConvertXamlToData(DocumentViewModel xaml, object parameter = null)
        {
            return xaml.DocumentController;
        }
    }
}
