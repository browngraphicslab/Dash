using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using static Dash.CourtesyDocuments;

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

            public NoteDocument(DocumentType type)
            {
                Type = type; 
            }

            //public abstract FrameworkElement MakeView(); 
            public virtual FrameworkElement MakeView()
            {
                DocumentViewModel vm = new DocumentViewModel(Document);
                return new DocumentView(vm);
            }
        }

        
        public class RichTextNote : NoteDocument
        {
            public static Key TitleKey = new Key("EF1B8247-B31F-4821-859C-9E28FDD098D3", "Title");
            public static Key RTFieldKey = new Key("0DBA83CB-D75B-4FCE-BBF0-9778B182836F", "RichTextField");
            static DocumentController _prototypeRTFNote = CreatePrototypeRTF();
            static DocumentController _prototypeLayout = CreatePrototypeLayout();

            private static DocumentController CreatePrototypeRTF()
            {
                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(TitleKey, new TextFieldModelController("Prototype Title"));
                fields.Add(RTFieldKey, new RichTextFieldModelController("Prototype Content"));
                return new DocumentController(fields, Type);
            }

            private static DocumentController CreatePrototypeLayout()
            {
                var titleLayout = new TextingBox(new DocumentReferenceController(_prototypeRTFNote.GetId(), TitleKey), 0, 0, 200, 50);
                var richTextLayout = new RichTextBox(new DocumentReferenceController(_prototypeRTFNote.GetId(), RTFieldKey), 0, 50, 200, 200);
                var prototpeLayout = new StackingPanel(new DocumentController[] { titleLayout.Document, richTextLayout.Document }, true);

                return prototpeLayout.Document;
            }

            public RichTextNote(DocumentType type) : base(type)
            {
                Document = _prototypeRTFNote.MakeDelegate();
                Document.SetField(TitleKey, new TextFieldModelController("Title?"), true);
                Document.SetField(RTFieldKey, new RichTextFieldModelController("Something to fill this space?"), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout);
            }
            /* 
            public override FrameworkElement MakeView()
            {
                /* 
                var fields = new Dictionary<Key, FieldModelController>
                {
                    [DocumentCollectionFieldModelController.CollectionKey] =
               new DocumentCollectionFieldModelController(new[]
                   {numbers, twoImages2})
                };

                var col = new DocumentController(fields, new DocumentType("collection", "collection"));
                var layoutDoc =
                    new CourtesyDocuments.CollectionBox(new DocumentReferenceController(col.GetId(),
                        DocumentCollectionFieldModelController.CollectionKey)).Document;
                var layoutController = new DocumentFieldModelController(layoutDoc);
                col.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutController, true);
                col.SetField(DashConstants.KeyStore.LayoutListKey, new DocumentCollectionFieldModelController(new List<DocumentController> { layoutDoc }), true);
                DisplayDocument(col);
                
        }*/
    }

        public class ImageNote : NoteDocument
        {
            public ImageNote(DocumentType type) : base(type)
            {

            }
        }


        public class PostitNote : NoteDocument
        {
            public static Key NotesFieldKey = new Key("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            static DocumentController _prototypePostit = CreatePrototypePostit();
            static DocumentController _prototypeLayout = CreatePrototypeLayout();

            private static DocumentController CreatePrototypePostit()
            {
                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(NotesFieldKey, new TextFieldModelController("Prototype Text"));
                return new DocumentController(fields, Type);
            }

            private static DocumentController CreatePrototypeLayout()
            {
                var prototypeTextLayout =
                    new TextingBox(new DocumentReferenceController(_prototypePostit.GetId(), NotesFieldKey), 0, 0, double.NaN, double.NaN);

                return prototypeTextLayout.Document;
            }

            public PostitNote(DocumentType type) : base(type)
            {
                Document = _prototypePostit.MakeDelegate();
                Document.SetField(NotesFieldKey, new TextFieldModelController("Hello World!"), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                
                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout);
            }
        }

        
    }
}
