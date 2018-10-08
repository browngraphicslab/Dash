using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.references)]
    public class PdfToReferencesOperatorController : OperatorController
    {
        public PdfToReferencesOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public PdfToReferencesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Extract title references from a pdf", new Guid("CF845204-1472-41F6-9939-64E88521B0CB"));

        //Input keys
        public static readonly KeyController PdfKey = new KeyController("PDFDoc");

        //Output keys
        public static readonly KeyController ComputedResultKey = new KeyController("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PdfKey, new IOInfo(TypeInfo.Document, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ComputedResultKey] = TypeInfo.Document
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            FieldControllerBase source = inputs[PdfKey];
            if (!(source is DocumentController pdfDoc && pdfDoc.DocumentType.Equals(PdfBox.DocumentType))) return Task.CompletedTask;
            
            string documentText = pdfDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null).Data.Replace("ACM Trans. Graph. 29,", "ACM Trans. Graph. 29.");

            var reg = new Regex(@"(?:\.|(?:References)) (?'authors'[A-Z,\.ØÜ\- ]+\.) (?'dates'[0-9]{4})\.[\s](?'title'[\w\n:\-,\s\(\)]+)\.");
            MatchCollection matches = reg.Matches(documentText);

            var authors = matches.Select(m => new TextController(m.Groups[1].Value)).ToList();
            var dates = matches.Select(m => new TextController(m.Groups[2].Value)).ToList();
            var titles = matches.Select(m => new TextController(m.Groups[3].Value.Replace("- ", "").Replace("-", ""))).ToList();
            
            var referenceList = new ListController<DocumentController>();

            for (var i = 0; i < matches.Count; i++)
            {
                var infoDoc = new DocumentController();

                infoDoc.SetField<NumberController>(KeyStore.ReferenceNumKey, (i + 1).ToString(), true);
                infoDoc.SetField(KeyStore.TitleKey, titles[i], true);
                infoDoc.SetField(KeyStore.AuthorKey, authors[i], true);
                infoDoc.SetField(KeyStore.ReferenceDateKey, dates[i], true);

                referenceList.Add(infoDoc);
            }

            pdfDoc.SetField(KeyStore.ReferencesDictKey, referenceList, true);

            outputs[ComputedResultKey] = referenceList;
            return Task.CompletedTask;
        }

        public static string Normalize(string s)
        {
            string normalizedString = s.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        public override FieldControllerBase GetDefaultController() => new PdfToReferencesOperatorController();
    }
}
