using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;

namespace Dash
{
    public class TextToDashUtil : IFileParser
    {
        public static readonly KeyController FileTextKey = new KeyController("5E461F63-5361-4296-9986-E530151205B2", "File Text");
        public static readonly DocumentType TextFileDocumentType = new DocumentType("2A77CA39-B058-49EC-B132-3775D705E977", "Text File");

        public async Task<DocumentController> ParseFileAsync(IStorageFile item, string uniquePath = null)
        {
            var text = await FileIO.ReadTextAsync(item);
            var fields = new Dictionary<KeyController, FieldControllerBase>()
            {
                [KeyStore.TitleKey] = new TextFieldModelController(item.Name),
                [FileTextKey] = new TextFieldModelController(text)
            };
            var doc = new DocumentController(fields, TextFileDocumentType);
            doc.SetActiveLayout(new DefaultLayout(0, 0, 200, 200).Document, true, true);
            return doc;
        }
    }
}