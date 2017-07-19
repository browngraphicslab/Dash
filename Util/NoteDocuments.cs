using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dash.CourtesyDocuments;

namespace Dash
{
    /// <summary>
    /// Class that makes document types that the user can take notes with 
    /// </summary>
    public static class NoteDocuments
    {
        public abstract class NoteDocument {
            
            protected DocumentController Document { get; set; }

            public NoteDocument(DocumentType type)
            {
                Document = new DocumentController(new Dictionary<Key, FieldModelController>(), type); 
            }
            
        }

        public class RichTextNote : NoteDocument
        {
            public static Key TitleKey = new Key("EF1B8247-B31F-4821-859C-9E28FDD098D3", "Title");
            public static Key RTFieldKey = new Key("0DBA83CB-D75B-4FCE-BBF0-9778B182836F", "RichTextField");

            public RichTextNote(DocumentType type) : base(type)
            {
                var titleLayout = new TextingBox(new DocumentReferenceController(Document.GetId(), TitleKey), 0, 0, 200, 50);
                var richTextLayout = new RichTextBox(new DocumentReferenceController(Document.GetId(), RTFieldKey), 0, 50, 200, 200);

                var activeLayout = new StackingPanel(new DocumentController[] { titleLayout.Document, richTextLayout.Document }, true);
                Document.SetActiveLayout(activeLayout.Document);  
            }
        }

        /* 
        public class PostitNote : NoteDocument
        {
            public static DocumentType PostitNoteType =
                new DocumentType("A5FEFB00-EA2C-4B64-9230-BBA41BACCAFC", "Post It");

            public static Key NotesFieldKey = new Key("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            static DocumentController _prototypePostit = CreatePrototypePostit();
            static DocumentController _prototypeLayout = CreatePrototypeLayout();

            static DocumentController CreatePrototypePostit()
            {
                // bcz: default values for data fields can be added, but should not be needed
                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(NotesFieldKey, new TextFieldModelController("Prototype Text"));
                return new DocumentController(fields, PostitNoteType);
            }
            static DocumentController CreatePrototypeLayout()
            {
                var prototypeTextLayout =
                    //new StackingPanel(new DocumentController[] {
                    new TextingBox(new DocumentReferenceController(_prototypePostit.GetId(), NotesFieldKey), 0, 0, double.NaN, double.NaN);
                //});

                return prototypeTextLayout.Document;
            }

            public PostitNote()
            {

                Document = _prototypePostit.MakeDelegate();
                Document.SetField(NotesFieldKey, new TextFieldModelController("Hello World!"), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                SetLayoutForDocument(Document, docLayout, true); // this is the only call which makes postit a courtesy document
            }
        }
        */
    }
}
