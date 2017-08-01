﻿using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Converters
{
    public class DocumentCollectionToStringConverter : SafeDataToXamlConverter<List<DocumentController>, string>
    {

        public DocumentCollectionToStringConverter()
        {
        }

        string GetPrimaryKeyString(DocumentController data)
        {
            var keyList = data.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = data.GetDereferencedField(new Key((k as TextFieldModelController).Data), null);
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
                var doc = new DocumentControllerToStringConverter().ConvertXamlToData(d + '>');
                if (doc != null)
                    docList.Add(doc);
            }
            return docList;
        }
    }

    public class DocumentControllerToStringConverter : SafeDataToXamlConverter<DocumentController, string>
    {
        private DocumentController _doc;

        public DocumentControllerToStringConverter()
        {
        }

        public override string ConvertDataToXaml(DocumentController data, object parameter = null)
        {
            _doc = data;
            var keyList = data.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                var docString = "<";
                foreach (var k in keys.Data)
                {
                    var keyField = data.GetDereferencedField(new Key((k as TextFieldModelController).Data), null);
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
            var keyList = _doc?.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null);
            var keys = keyList as ListFieldModelController<TextFieldModelController>;
            if (keys != null)
            {
                foreach (var dmc in ContentController.GetControllers<DocumentController>())
                    if (!dmc.DocumentType.Type.Contains("Box") &&
                        dmc.DocumentType != StackLayout.DocumentType &&
                        dmc.DocumentType != GridLayout.GridPanelDocumentType &&
                        dmc.DocumentType != GridViewLayout.DocumentType)
                    {
                        bool found = true;
                        foreach (var k in keys.Data)
                        {
                            var key = new Key((k as TextFieldModelController).Data);
                            var index = keys.Data.IndexOf(k);
                            var derefValue = (dmc.GetDereferencedField(key, null) as TextFieldModelController)?.Data;
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
            foreach (var dmc in ContentController.GetControllers<DocumentController>())
                if (!dmc.DocumentType.Type.Contains("Box") && !dmc.DocumentType.Type.Contains("Layout"))
                {
                    var primaryKeys = dmc.GetDereferencedField(DashConstants.KeyStore.PrimaryKeyKey, null) as ListFieldModelController<TextFieldModelController>;
                    if (primaryKeys != null)
                    {
                        bool found = true;
                        foreach (var value in values)
                        {
                            bool foundValue = false;
                            foreach (var kf in primaryKeys.Data)
                            {
                                var key = new Key((kf as TextFieldModelController).Data);
                                var derefValue = (dmc.GetDereferencedField(key, null) as TextFieldModelController)?.Data;
                                if (derefValue != null)
                                {
                                    if (value == derefValue)
                                    {
                                        foundValue = true;
                                        break;
                                    }
                                }
                            }
                            if (!foundValue)
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
            return DBTest.DBNull;
        }
    }
}
