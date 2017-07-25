using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

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

            protected DocumentController GetLayoutPrototype()
            {
                var prototype = ContentController.GetController<DocumentController>(_prototypeID);
                if (prototype == null)
                {
                    prototype = CreatePrototype(); // TODO should this be CreatePrototypeLayout ..?
                }
                return prototype;
            }

        }
        
        public class RichTextNote : NoteDocument
        {
            public static Key TitleKey = new Key("EF1B8247-B31F-4821-859C-9E28FDD098D3", "Title");
            public static Key RTFieldKey = new Key("0DBA83CB-D75B-4FCE-BBF0-9778B182836F", "RichTextField");


            public override DocumentController CreatePrototype()
            {

                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(TitleKey, new TextFieldModelController("Prototype Title"));
                fields.Add(RTFieldKey, new RichTextFieldModelController("Prototype Content"));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetLayoutPrototype(); 
                var titleLayout = new TextingBox(new ReferenceFieldModelController(prototype.GetId(), TitleKey), 0, 0, 200, 50);
                var richTextLayout = new RichTextBox(new ReferenceFieldModelController(prototype.GetId(), RTFieldKey), 0, 50, 200, 200);
                var prototpeLayout = new StackingPanel(new DocumentController[] { titleLayout.Document, richTextLayout.Document }, true);

                return prototpeLayout.Document;
            }

            public RichTextNote(DocumentType type) : base(type)
            {
                _prototypeID = "A79BB20B-A0D0-4F5C-81C6-95189AF0E90D";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetLayoutPrototype().MakeDelegate();
                Document.SetField(TitleKey, new TextFieldModelController("Title?"), true);
                Document.SetField(RTFieldKey, new RichTextFieldModelController("Something to fill this space?"), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout, true, true);
            }
        }

        public class ImageNote : NoteDocument
        {
            public static Key TitleKey = new Key("290976B3-5FFA-4899-97B8-7DBFFF7C2E4A", "Title");
            public static Key IamgeFieldKey = new Key("FAE62A35-F463-4FE5-9E8D-CDE6DFEB5E20", "RichTextField");

            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(TitleKey, new TextFieldModelController("Prototype Title"));
                fields.Add(IamgeFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototype = GetLayoutPrototype();

                var titleLayout = new TextingBox(new ReferenceFieldModelController(prototype.GetId(), TitleKey), 0, 0, 200, 50);
                var imageLayout = new ImageBox(new ReferenceFieldModelController(prototype.GetId(), IamgeFieldKey), 0, 50, 200, 200);
                var prototpeLayout = new StackingPanel(new DocumentController[] { titleLayout.Document, imageLayout.Document }, true);

                return prototpeLayout.Document;
            }

            public ImageNote(DocumentType type) : base(type)
            {
                _prototypeID = "C48C8AF2-5609-40F0-9FAA-E300C582AF5F";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetLayoutPrototype().MakeDelegate();
                Document.SetField(TitleKey, new TextFieldModelController("Title?"), true);
                Document.SetField(IamgeFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);

                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout, true, true);
            }
        }

        public class PostitNote : NoteDocument
        {
            public static Key NotesFieldKey = new Key("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            public static DocumentType DocumentType = new DocumentType("4C20B539-BF40-4B60-9FA4-2CC531D3C757", "Post it Note");

            public override DocumentController CreatePrototype()
            {
                var fields = new Dictionary<Key, FieldModelController>();
                fields.Add(NotesFieldKey, new TextFieldModelController("Prototype Text"));
                return new DocumentController(fields, Type, _prototypeID);
            }

            public override DocumentController CreatePrototypeLayout()
            {
                var prototypeTextLayout =
                    new TextingBox(new ReferenceFieldModelController(GetLayoutPrototype().GetId(), NotesFieldKey), 0, 0, double.NaN, double.NaN);

                return prototypeTextLayout.Document;
            }

            public PostitNote(DocumentType type) : base(type)
            {
                _prototypeID = "08AC0453-D39F-45E3-81D9-C240B7283BCA";
                _prototypeLayout = CreatePrototypeLayout();

                Document = GetLayoutPrototype().MakeDelegate();
                Document.SetField(NotesFieldKey, new TextFieldModelController("Write something amazing!"), true);

                var docLayout = _prototypeLayout.MakeDelegate();
                docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
                var widthController = docLayout.GetWidthField();
                widthController.Data = double.NaN;
                var heightController = docLayout.GetHeightField();
                heightController.Data = double.NaN;
                docLayout.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                
                Document.AddLayoutToLayoutList(docLayout);
                Document.SetActiveLayout(docLayout,true, true);
            }
        }

    }
}
