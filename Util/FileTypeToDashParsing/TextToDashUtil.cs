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
            var doc = new NoteDocuments.PostitNote(text, item.Name).Document;
            return doc;
        }
    }
}