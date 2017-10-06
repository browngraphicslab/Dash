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
                var prototype = ContentController<DocumentModel>.GetController<DocumentController>(_prototypeID);
                if (prototype == null)
                {
                    prototype = CreatePrototype(); // TODO should this be CreatePrototypeLayout ..?
                    prototype.SetField(KeyStore.ThisKey, new DocumentFieldModelController(prototype), true);
                }
                return prototype;
            }

        }

        public class CollectionNote : NoteDocument
        {
            public static KeyController CollectedDocsKey = new KeyController("F12AEF6B-C302-45D6-B0B8-A9906EF16DAF", "Collected Docs");


            public override DocumentController CreatePrototype()
            {

                var fields = new Dictionary<KeyController, FieldControllerBase>();
                fields.Add(CollectedDocsKey, new DocumentCollectionFieldModelController());
                fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Collected Docs Note Data API"));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype();
                var prototpeLayout = new CollectionBox(new DocumentReferenceFieldController(prototype.GetId(), CollectedDocsKey), 0, 0, double.NaN, double.NaN);
                prototpeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
                prototpeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(400), true);
                prototpeLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                prototpeLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);

                return prototpeLayout.Document;
            }
            public static DocumentType DocumentType = new DocumentType("EDDED871-DD89-4E6E-9C5E-A1CF927B3CB2", "Collected Docs Note");
            public DocumentController DataDocument { get; set; }
            public CollectionNote(Point where, CollectionView.CollectionViewType viewtype,  string title = "", DocumentController collectedDocument = null) : base(DocumentType)
            {
                _prototypeID = "03F76CDF-21F1-404A-9B2C-3377C025DA0A";

                var dataDocument = GetDocumentPrototype().MakeDelegate();
                dataDocument.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dataDocument), true);

                if (_prototypeLayout == null)
                    _prototypeLayout = CreatePrototypeLayout();
                var docLayout = CreatePrototypeLayout();// _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(where), true);
                docLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
                docLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(300), true);
                docLayout.SetField(CollectionBox.CollectionViewTypeKey, new TextFieldModelController(viewtype.ToString()), true);

                var listOfCollectedDocs = collectedDocument != null ? new List<DocumentController>(new DocumentController[] { collectedDocument }) : new List<DocumentController>();
                dataDocument.SetField(CollectionNote.CollectedDocsKey, new DocumentCollectionFieldModelController(listOfCollectedDocs), true);
                
                if (true)
                {
                    dataDocument.AddLayoutToLayoutList(docLayout);
                    dataDocument.SetActiveLayout(docLayout, true, true);
                    Document = dataDocument;
                }
                else
                {
                    docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(dataDocument), true);
                    Document = docLayout;
                }
                Document.SetField(KeyStore.PrimaryKeyKey,       new DocumentCollectionFieldModelController(listOfCollectedDocs), true);
                Document.SetField(AnnotatedImage.TitleFieldKey, new TextFieldModelController(title), true);
                Document.SetField(KeyStore.ThumbnailFieldKey,   new DocumentFieldModelController(listOfCollectedDocs.FirstOrDefault()), true);
                DataDocument = dataDocument;
            }
        }
        public class RichTextNote : NoteDocument
        {
            public static KeyController TitleKey = new KeyController("EF1B8247-B31F-4821-859C-9E28FDD098D3", "Title");
            public static KeyController RTFieldKey = new KeyController("0DBA83CB-D75B-4FCE-BBF0-9778B182836F", "RichTextField");


            public override DocumentController CreatePrototype()
            {

                var fields = new Dictionary<KeyController, FieldControllerBase>();
                fields.Add(TitleKey, new TextFieldModelController("Prototype Title"));
                fields.Add(RTFieldKey, new RichTextFieldModelController(new RichTextFieldModel.RTD("Prototype Content")));
                fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("RichText Note Data API"));
                fields.Add(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(
                    new TextFieldModelController[] { new TextFieldModelController(TitleKey.Id) }));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype(); 
                var titleLayout = new TextingBox(new DocumentReferenceFieldController(prototype.GetId(), TitleKey), 0, 0, double.NaN, 25, null, Colors.LightBlue);
                var richTextLayout = new RichTextBox(new DocumentReferenceFieldController(prototype.GetId(), RTFieldKey), 0, 0, double.NaN, double.NaN);
                var prototpeLayout = new StackLayout(new DocumentController[] { titleLayout.Document, richTextLayout.Document });
                prototpeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
                prototpeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(400), true);
                prototpeLayout.Document.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                prototpeLayout.Document.SetVerticalAlignment(VerticalAlignment.Stretch);

                return prototpeLayout.Document;
            }
            
            public RichTextNote(DocumentType type) : base(type)
            {
                _prototypeID = "A79BB20B-A0D0-4F5C-81C6-95189AF0E90D";

                var dataDocument = GetDocumentPrototype().MakeDelegate();
                dataDocument.SetField(TitleKey, new TextFieldModelController("Title?"), true);
                dataDocument.SetField(RTFieldKey, new RichTextFieldModelController(new RichTextFieldModel.RTD("Something to fill this space?")), true);
                dataDocument.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dataDocument), true);

                if (_prototypeLayout == null)
                    _prototypeLayout = CreatePrototypeLayout();
                var docLayout = CreatePrototypeLayout();// _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                if (false)
                {
                    dataDocument.AddLayoutToLayoutList(docLayout);
                    dataDocument.SetActiveLayout(docLayout, true, true);
                    Document = dataDocument;
                } else
                {
                    docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(dataDocument), true);
                    docLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
                    docLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(400), true);
                    Document = docLayout;
                }
            }
        }

        public class ImageNote : NoteDocument
        {
            public static KeyController TitleKey = new KeyController("290976B3-5FFA-4899-97B8-7DBFFF7C2E4A", "Title");
            public static KeyController IamgeFieldKey = new KeyController("FAE62A35-F463-4FE5-9E8D-CDE6DFEB5E20", "RichTextField");

            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>();
                fields.Add(TitleKey, new TextFieldModelController("Prototype Title"));
                fields.Add(IamgeFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetDocumentPrototype();

                var titleLayout = new TextingBox(new DocumentReferenceFieldController(prototype.GetId(), TitleKey), 0, 0, 200, 50);
                var imageLayout = new ImageBox(new DocumentReferenceFieldController(prototype.GetId(), IamgeFieldKey), 0, 50, 200, 200);
                var prototpeLayout = new StackLayout(new DocumentController[] { titleLayout.Document, imageLayout.Document }, true);

                return prototpeLayout.Document;
            }

            public ImageNote(DocumentType type) : base(type)
            {
                _prototypeID = "C48C8AF2-5609-40F0-9FAA-E300C582AF5F";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetDocumentPrototype().MakeDelegate();
                Document.SetField(TitleKey, new TextFieldModelController("Title?"), true);
                Document.SetField(IamgeFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout, true, true);
            }
        }

        public class PostitNote : NoteDocument
        {
            public static KeyController NotesFieldKey = new KeyController("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            public static DocumentType DocumentType = new DocumentType("4C20B539-BF40-4B60-9FA4-2CC531D3C757", "Post it Note");

            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>();
                fields.Add(NotesFieldKey, new TextFieldModelController("Prototype Text"));
                fields.Add(KeyStore.AbstractInterfaceKey, new TextFieldModelController("Post-It Data API" ));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototypeTextLayout =
                    new TextingBox(new DocumentReferenceFieldController(GetDocumentPrototype().GetId(), NotesFieldKey), 0, 0, double.NaN, double.NaN);
                prototypeTextLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(400), true);
                prototypeTextLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);

                return prototypeTextLayout.Document;
            }

            public PostitNote(DocumentType type) : base(type)
            {
                _prototypeID = "08AC0453-D39F-45E3-81D9-C240B7283BCA";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetDocumentPrototype().MakeDelegate();
                Document.SetField(NotesFieldKey, new TextFieldModelController("Write something amazing!"), true);
                Document.SetField(KeyStore.ThisKey, new DocumentFieldModelController(Document), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                docLayout.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                
                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout,true, true);
            }
        }

    }
}
