using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Dash.Controllers;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// Class that makes document types that the user can take notes with 
    /// </summary>
    public static class NoteDocuments
    {
        public abstract class NoteDocument {

            public DocumentController Document { get; set; }

            protected static DocumentType Type { get; set;}

            public static DocumentController _prototype;
            public static DocumentController _prototypeLayout;

            protected static string _prototypeID;
            public NoteDocument(DocumentType type)
            {
                Type = type;
                //_prototype = CreatePrototype();
                //_prototypeLayout = CreatePrototypeLayout(); 
            }

            public abstract DocumentController CreatePrototype();
            public abstract DocumentController CreatePrototypeLayout();

            protected DocumentController GetDocumentPrototype()
            {
                var prototype = ContentController<FieldModel>.GetController<DocumentController>(_prototypeID);
                if (prototype == null)
                {
                    prototype = CreatePrototype(); // TODO should this be CreatePrototypeLayout ..?
                    prototype.SetField(KeyStore.ThisKey, prototype, true);
                }
                return prototype;
            }

        }

        public class CollectionNote : NoteDocument
        {
            public static KeyController CollectedDocsKey = new KeyController("F12AEF6B-C302-45D6-B0B8-A9906EF16DAF", "Collected Docs");


            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>()
                {
                    [CollectedDocsKey] = new ListController<DocumentController>(),
                    [KeyStore.AbstractInterfaceKey] = new TextController("Collected Docs Note Data API"),
                    [KeyStore.TitleKey] = new TextController("Collection Note"),
                    [KeyStore.PrimaryKeyKey] = new ListController<TextController>(new TextController[] { new TextController(KeyStore.TitleKey.Id) })
                };
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype();
                var prototypeLayout = new CollectionBox(new DocumentReferenceController(prototype.GetId(), CollectedDocsKey), 0, 0, double.NaN, double.NaN);
                prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                prototypeLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);

                return prototypeLayout.Document;
            }
            public static DocumentType DocumentType = new DocumentType("EDDED871-DD89-4E6E-9C5E-A1CF927B3CB2", "Collected Docs Note");
            public DocumentController DataDocument { get; set; }
            public CollectionNote(Point where, CollectionView.CollectionViewType viewtype,  string title = "", double width=200, double height = 300, List<DocumentController> collectedDocuments = null) : base(DocumentType)
            {
                _prototypeID = "03F76CDF-21F1-404A-9B2C-3377C025DA0A";

                var dataDocument = GetDocumentPrototype().MakeDelegate();
                dataDocument.SetField(KeyStore.ThisKey, dataDocument, true);
                dataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);

                if (_prototypeLayout == null)
                    _prototypeLayout = CreatePrototypeLayout();
                var docLayout = CreatePrototypeLayout();// _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointController(where), true);
                docLayout.SetField(KeyStore.WidthFieldKey, new NumberController(width), true);
                docLayout.SetField(KeyStore.HeightFieldKey, new NumberController(height), true);
                docLayout.SetField(KeyStore.CollectionViewTypeKey, new TextController(viewtype.ToString()), true);

                var listOfCollectedDocs = collectedDocuments ?? new List<DocumentController>();

                dataDocument.SetField(CollectionNote.CollectedDocsKey, new ListController<DocumentController>(listOfCollectedDocs), true);
                
                if (false)
                {
                    dataDocument.AddLayoutToLayoutList(docLayout);
                    dataDocument.SetActiveLayout(docLayout, true, true);
                    Document = dataDocument;
                }
                else
                {
                    docLayout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
                    Document = docLayout;
                }
                if (listOfCollectedDocs.Any())
                {
                    Document.SetField(KeyStore.ThumbnailFieldKey,  listOfCollectedDocs.FirstOrDefault(), true);
                }
                DataDocument = dataDocument;
            }
        }
        public class RichTextNote : NoteDocument
        {
            public static KeyController RTFieldKey = new KeyController("0DBA83CB-D75B-4FCE-BBF0-9778B182836F", "RichTextField");
            
            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.TitleKey]             = new TextController("Prototype Title"),
                    [RTFieldKey]                    = new RichTextController(new RichTextModel.RTD("Prototype Content")),
                    [KeyStore.AbstractInterfaceKey] = new TextController("RichText Note Data API"),
                    [KeyStore.PrimaryKeyKey]        = new ListController<TextController>(
                            new TextController[] {new TextController(KeyStore.TitleKey.Id)})
                };
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype(); 
                var titleLayout = new TextingBox(new DocumentReferenceController(prototype.GetId(), KeyStore.TitleKey), 0, 0, double.NaN, 25, null, Colors.LightBlue);
                var richTextLayout = new RichTextBox(new DocumentReferenceController(prototype.GetId(), RTFieldKey), 0, 0, double.NaN, double.NaN);
                var prototypeLayout = new StackLayout(new DocumentController[] { titleLayout.Document, richTextLayout.Document });
                prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                prototypeLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);

                return prototypeLayout.Document;
            }
            
            public RichTextNote(DocumentType type, string title = "Title?") : base(type)
            {
                _prototypeID = "A79BB20B-A0D0-4F5C-81C6-95189AF0E90D";

                var dataDocument = GetDocumentPrototype().MakeDelegate();
                dataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);
                dataDocument.SetField(RTFieldKey, new RichTextController(new RichTextModel.RTD("Something to fill this space?")), true);
                dataDocument.SetField(KeyStore.ThisKey, dataDocument, true);

                if (_prototypeLayout == null)
                    _prototypeLayout = CreatePrototypeLayout();
                var docLayout = CreatePrototypeLayout();// _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);

                if (false)
                {
                    dataDocument.AddLayoutToLayoutList(docLayout);
                    dataDocument.SetActiveLayout(docLayout, true, true);
                    Document = dataDocument;
                } else
                {
                    docLayout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
                    docLayout.SetField(KeyStore.WidthFieldKey, new NumberController(400), true);
                    docLayout.SetField(KeyStore.HeightFieldKey, new NumberController(400), true);
                    Document = docLayout;
                }
            }
        }

        public class ImageNote : NoteDocument
        {
            public static KeyController ImageFieldKey = new KeyController("FAE62A35-F463-4FE5-9E8D-CDE6DFEB5E20", "RichTextField");

            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    {KeyStore.TitleKey, new TextController("Prototype Title")},
                    {ImageFieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat2.jpeg"))}
                };
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype();

                var titleLayout = new TextingBox(new DocumentReferenceController(prototype.GetId(), KeyStore.TitleKey), 0, 0, 200, 50);
                var imageLayout = new ImageBox(new DocumentReferenceController(prototype.GetId(), ImageFieldKey), 0, 50, 200, 200);
                var prototpeLayout = new StackLayout(new DocumentController[] { titleLayout.Document, imageLayout.Document }, true);

                return prototpeLayout.Document;
            }

            public ImageNote(DocumentType type) : base(type)
            {
                _prototypeID = "C48C8AF2-5609-40F0-9FAA-E300C582AF5F";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetDocumentPrototype().MakeDelegate();
                Document.SetField(KeyStore.TitleKey, new TextController("Title?"), true);
                Document.SetField(ImageFieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);

                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout, true, true);
            }
        }

        public class PostitNote : NoteDocument
        {
            public static KeyController NotesFieldKey = new KeyController("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            public static DocumentType DocumentType = new DocumentType("4C20B539-BF40-4B60-9FA4-2CC531D3C757", "Text Note");
            
            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.TitleKey]             = new TextController("Prototype Title"),
                    [KeyStore.DocumentTextKey]      = new TextController("Prototype Content"),
                    [KeyStore.AbstractInterfaceKey] = new TextController("PostIt Note Data API"),
                    [KeyStore.PrimaryKeyKey]        = new ListController<TextController>(
                            new TextController[] {new TextController(KeyStore.TitleKey.Id)})
                };
                return new DocumentController(fields, DocumentType, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype();
                var titleLayout = new TextingBox(new DocumentReferenceController(prototype.GetId(), KeyStore.TitleKey), 0, 0, double.NaN, 25, null, Colors.LightBlue);
                var textLayout  = new TextingBox(new DocumentReferenceController(prototype.GetId(), KeyStore.DocumentTextKey), 0, 0, double.NaN, double.NaN);
                var prototypeLayout = new StackLayout(new DocumentController[] { titleLayout.Document, textLayout.Document });
                prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(400), true);
                prototypeLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                prototypeLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);

                return prototypeLayout.Document;
            }


            // TODO for bcz - takes in text and title to display, docType is by default the one stored in this class
            public PostitNote(string text = null, string title = null, DocumentType type = null) : base(type ?? DocumentType)
            {
                _prototypeID = "08AC0453-D39F-45E3-81D9-C240B7283BCA";

                if (_prototypeLayout == null)
                    _prototypeLayout = CreatePrototypeLayout();
                var docLayout = CreatePrototypeLayout();// _prototypeLayout.MakeDelegate();
                _prototypeLayout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);

                var dataDocument = GetDocumentPrototype().MakeDelegate();
                dataDocument.SetField(KeyStore.TitleKey, new TextController(title), true);
                dataDocument.SetField(KeyStore.DocumentTextKey, new TextController(text ?? "Write something amazing!"), true);
                dataDocument.SetField(KeyStore.ThisKey, dataDocument, true);
                

                if (false)
                {
                    dataDocument.AddLayoutToLayoutList(docLayout);
                    dataDocument.SetActiveLayout(docLayout, true, true);
                    Document = dataDocument;
                }
                else
                {
                    docLayout.SetField(KeyStore.DocumentContextKey, dataDocument, true);
                    docLayout.SetField(KeyStore.WidthFieldKey, new NumberController(400), true);
                    docLayout.SetField(KeyStore.HeightFieldKey, new NumberController(400), true);
                    Document = docLayout;
                }
            }
        }

    }
}
