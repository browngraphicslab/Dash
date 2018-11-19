using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string DocumentToParseableString(DocumentController data)
        {
            return "<" + data.Id + ">";
        }

        public override string ConvertDataToXaml(List<DocumentController> dataList, object parameter = null)
        {
            if (_returnCount)
                return dataList.Count().ToString();
            var docListString = "{";
            foreach (var data in dataList)
            {
                docListString += DocumentToParseableString(data) + ",";
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
            return DocumentCollectionToStringConverter.DocumentToParseableString(data);
        }

        public override DocumentController ConvertXamlToData(string xaml, object parameter = null)
        {
            var values = xaml.TrimStart('<').TrimEnd('>').Trim();
            return RESTClient.Instance.Fields.GetController<DocumentController>(values);
        }
    }
}
