using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Windows.Foundation;
using Dash.Controllers;

namespace Dash
{
	class TemplateNote : NoteDocument
	{
		public static DocumentType DocumentType = new DocumentType("0BD8E9E2-D414-4AD3-9D33-98BA185510A2", "Template Note");
		static string _prototypeID = "004CB4BF-AB4D-4600-AD92-3AF31AFFD10B";

		protected override DocumentController createPrototype(string prototypeID)
		{
			var fields = new Dictionary<KeyController, FieldControllerBase>
			{
				[KeyStore.AbstractInterfaceKey] = new TextController("Template Data API"),
			};
			var protoDoc = new DocumentController(fields, DocumentType, prototypeID) { Tag = "Template Editor Data Prototype" };

			protoDoc.SetField(KeyStore.DocumentTextKey, new DocumentReferenceController(protoDoc.Id, RichTextDocumentOperatorController.ReadableTextKey), true);
			protoDoc.SetField(KeyStore.TitleKey, new DocumentReferenceController(protoDoc.Id, RichTextTitleOperatorController.ComputedTitle), true);
			return protoDoc;
		}

		static int rcount = 1;
		DocumentController CreateLayout(DocumentController dataDoc, Point @where, Size size)
		{
			size = new Size(size.Width == 0 ? double.NaN : size.Width, size.Height == 0 ? double.NaN : size.Height);
			return new TemplateBox(getDataReference(dataDoc), where.X, where.Y, size.Width, size.Height).Document;
		}

		public TemplateNote(Point where = new Point(), Size size = new Size()) :
			base(_prototypeID)
		{
			var dictionary = new Dictionary<KeyController, FieldControllerBase>
			{
				[KeyStore.DataKey] = new ListController<DocumentController>()
			};
			var controller = new DocumentController(dictionary, DocumentType);
			var dataDocument = makeDataDelegate(controller);
			Document = initSharedLayout(CreateLayout(dataDocument, where, size), dataDocument);
			//Document.SetField(KeyStore.TemplateDocumentKey, linkedToDoc.ViewModel.DataDocument, true);
			Document.Tag = "Template Data " + rcount;
			dataDocument.Tag = "Template Data" + rcount++;
		}
	}
}
