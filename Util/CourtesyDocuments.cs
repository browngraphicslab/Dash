using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Sources.Api;
using DashShared;
using Dash.Sources.Api.XAML_Elements;
using static Dash.Sources.Api.XAML_Elements.ApiProperty;

namespace Dash
{
    public static class CourtesyDocuments
    {
        /// <summary>
        /// This class provides base functionality for creating and displaying new documents.
        /// </summary>
        public class CourtesyDocument
        {
            public virtual DocumentController Document { get; set; }
            public static void SetLayoutForDocument(DocumentController document, DocumentModel layoutDoc)
            {
                var documentFieldModel = new DocumentModelFieldModel(layoutDoc);
                ContentController.AddModel(documentFieldModel);
                var layoutController = new DocumentFieldModelController(documentFieldModel);
                ContentController.AddController(layoutController);
                document.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);
            }

            /// <summary>
            /// Gives a layout document delegate both a Data field and a Layout field which override their prototypes' fields.
            /// The Data field is specifies the layout field instance documents that are needed to render the delegate
            /// The Layout field specifies that delegate will render itself instead of creating dynamic render instances for each of its fields
            /// </summary>
            /// <param name="prototypeLayout"></param>
            /// <param name="layoutDocs"></param>
            /// <returns></returns>
            public static DocumentController CreateDelegateLayout(DocumentController prototypeLayout, IEnumerable<DocumentModel> layoutDocs)
            {
                var deleg = prototypeLayout.MakeDelegate();

                var fm = new DocumentCollectionFieldModel(layoutDocs);
                ContentController.AddModel(fm);
                var fmc = new DocumentCollectionFieldModelController(fm);
                ContentController.AddController(fmc);
                var delg = prototypeLayout.MakeDelegate();

                deleg.SetField(DashConstants.KeyStore.DataKey, fmc, true);

                var selfFm = new DocumentModelFieldModel(deleg.DocumentModel);
                ContentController.AddModel(selfFm);
                var selfFmc = new DocumentFieldModelController(selfFm);
                ContentController.AddController(selfFmc);
                deleg.SetField(DashConstants.KeyStore.LayoutKey, selfFmc, true);
                return deleg;
            }
            public Dictionary<Key,FieldModel>  DefaultLayoutFields(double x, double y, double w, double h, FieldModel data)
            {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModel(w),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModel(h),
                    [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModel(x, y)
                };
                if (data != null)
                    fields.Add(DashConstants.KeyStore.DataKey, data);
                return fields;
            }
            public virtual List<FrameworkElement> makeView(DocumentController docController)
            {
                return new List<FrameworkElement>();
            }

            /// <summary>
            /// Adds bindings needed to create links between renderable fields on collections.
            /// </summary>
            /// <param name="refFieldModelController">A reference back to the data source for the <paramref name="renderElement"/></param>
            /// <param name="renderElement">The element which is actually rendered on the screen, this will receive bindings for interactions</param>
            protected static void BindOperationInteractions(ReferenceFieldModelController refFieldModelController, FrameworkElement renderElement)
            {
                renderElement.ManipulationMode = ManipulationModes.All;
                renderElement.ManipulationStarted += (sender, args) => args.Complete();
                renderElement.PointerPressed += delegate (object sender, PointerRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.StartDrag(new OperatorView.IOReference(refFieldModelController.ReferenceFieldModel, true, args, renderElement, renderElement.GetFirstAncestorOfType<DocumentView>()));
                };
                renderElement.PointerReleased += delegate (object sender, PointerRoutedEventArgs args)
                {
                    var view = renderElement.GetFirstAncestorOfType<CollectionView>();
                    if (view == null) return; // we can't always assume we're on a collection
                    view.EndDrag(new OperatorView.IOReference(refFieldModelController.ReferenceFieldModel, false, args, renderElement, renderElement.GetFirstAncestorOfType<DocumentView>()));
                };
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindHeight(FrameworkElement renderElement, NumberFieldModelController heightController)
            {
                if (heightController == null) throw new ArgumentNullException(nameof(heightController));
                var heightBinding = new Binding
                {
                    Source = heightController,
                    Path = new PropertyPath(nameof(heightController.Data)),
                    Mode = BindingMode.TwoWay
                };
                renderElement.SetBinding(FrameworkElement.HeightProperty, heightBinding);
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="NumberFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="NumberFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindWidth(FrameworkElement renderElement, NumberFieldModelController widthController)
            {
                if (widthController == null) throw new ArgumentNullException(nameof(widthController));
                var widthBinding = new Binding
                {
                    Source = widthController,
                    Path = new PropertyPath(nameof(widthController.Data)),
                    Mode = BindingMode.TwoWay
                };
                renderElement.SetBinding(FrameworkElement.WidthProperty, widthBinding);
            }

            /// <summary>
            /// Adds a binding from the passed in <see cref="renderElement"/> to the passed in <see cref="PointFieldModelController"/>
            /// <exception cref="ArgumentNullException">Throws an exception if the passed in <see cref="PointFieldModelController"/> is null</exception>
            /// </summary>
            protected static void BindTranslation(FrameworkElement renderElement, PointFieldModelController translateController)
            {
                if (translateController == null) throw new ArgumentNullException(nameof(translateController));
                var translateBinding = new Binding
                {
                    Source = translateController,
                    Path = new PropertyPath(nameof(translateController.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new PointToTranslateTransformConverter()
                };
                renderElement.SetBinding(UIElement.RenderTransformProperty, translateBinding);
            }

            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s height.
            /// </summary>
            protected static NumberFieldModelController GetHeightFieldController(DocumentController docController)
            {

                // make text height resize
                var heightController =
                    docController.GetField(DashConstants.KeyStore.HeightFieldKey) as NumberFieldModelController;
                Debug.Assert(heightController != null);
                return heightController;
            }

            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s width.
            /// </summary>
            protected static NumberFieldModelController GetWidthFieldController(DocumentController docController)
            {

                // make text width resize
                var widthController =
                    docController.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController;
                Debug.Assert(widthController != null);
                return widthController;
            }

            /// <summary>
            /// Returns the <see cref="NumberFieldModelController"/> from the passed in <see cref="DocumentController"/>
            /// used to control that <see cref="DocumentController"/>'s translation.
            /// </summary>
            protected static PointFieldModelController GetTranslateFieldController(DocumentController docController)
            {
                var translateController =
                    docController.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
                Debug.Assert(translateController != null);
                return translateController;
            }
        }

        /// <summary>
        /// Given a document, this provides an API for getting all of the layout documents that define it's view.
        /// </summary>
        public class LayoutCourtesyDocument : CourtesyDocument
        {
            DocumentController LayoutDocumentController = null;
            public LayoutCourtesyDocument(DocumentController docController)
            {
                Document = docController; // get the layout field on the document being displayed
                var layoutField = docController.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
                if (layoutField == null)
                {
                    var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModel(new DocumentModel[] { }));
                    LayoutDocumentController = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, CourtesyDocuments.GenericCollection.DocumentType)).GetReturnedDocumentController();

                    SetLayoutForDocument(Document, LayoutDocumentController.DocumentModel);
                }
                else
                    LayoutDocumentController = layoutField?.Data;
            }
            public IEnumerable<DocumentController> GetLayoutDocuments()
            {
                var layoutDataField = ContentController.DereferenceToRootFieldModel(LayoutDocumentController?.GetField(DashConstants.KeyStore.DataKey));
                if (layoutDataField is DocumentCollectionFieldModelController)
                    foreach (var d in (layoutDataField as DocumentCollectionFieldModelController).GetDocuments())
                        yield return d;
                else if (layoutDataField.FieldModel is DocumentModelFieldModel)
                    yield return ContentController.GetController<DocumentController>((layoutDataField.FieldModel as DocumentModelFieldModel).Data.Id);
                else yield return LayoutDocumentController;
            }
            public DocumentCollectionFieldModelController LayoutDocumentCollectionController = null;
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return LayoutCourtesyDocument.MakeView(docController);
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var docViewModel = new DocumentViewModel(docController)
                {
                    IsDetailedUserInterfaceVisible = false,
                    IsMoveable = false
                };
                return new List<FrameworkElement> { new DocumentView(docViewModel) };
            }
        }

        /// <summary>
        /// Given a reference to an operator field model, constructs a document type that displays that operator.
        /// </summary>
        public class OperatorBox : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

            public OperatorBox(ReferenceFieldModel refToOp)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, refToOp); 
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }

            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return OperatorBox.MakeView(docController);
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                ReferenceFieldModel rfm = (data as ReferenceFieldModelController).ReferenceFieldModel;
                OperatorView opView = new OperatorView { DataContext = rfm };
                return new List<FrameworkElement> { opView };
            }
        }

        /// <summary>
        /// A generic document type containing a single text element.
        /// </summary>
        public class TextingBox : CourtesyDocument
        {
            public static Key PrefixKey = new Key("AC1B4A0C-CFBF-43B3-B7F1-D7FC9E5BEEBE", "Text Prefix");
            public static Key FontWeightKey = new Key("03FC5C4B-6A5A-40BA-A262-578159E2D5F7", "FontWeight");
            public static DocumentType DocumentType = new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

            public DocumentController MakeDelegate(ReferenceFieldModel refModel)
            {
                ContentController.AddModel(refModel);
                var fmc = new ReferenceFieldModelController(refModel);
                ContentController.AddController(fmc);
                var delg = Document.MakeDelegate();
                delg.SetField(DashConstants.KeyStore.DataKey, fmc, true);
                return delg;
            }
            public TextingBox(FieldModel refToText, double x = 0, double y = 0, double w = 200, double h = 20)
            {
                var fields = DefaultLayoutFields(x, y, w, h, refToText);
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
                SetLayoutForDocument(Document, Document.DocumentModel);
            }
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return TextingBox.MakeView(docController);
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                // the text field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                // create the textblock
                FrameworkElement tb = null;

                // use the reference to the text to get the text field model controller
                var retToText = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(retToText != null);
                var fieldModelController = ContentController.DereferenceToRootFieldModel(retToText);
                if (fieldModelController is TextFieldModelController)
                {
                    tb = new TextBox();
                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    tb.VerticalAlignment = VerticalAlignment.Stretch;
                    var textFieldModelController = fieldModelController as TextFieldModelController;
                    Debug.Assert(textFieldModelController != null);
                    // make text update when changed
                    var sourceBinding = new Binding
                    {
                        Source = textFieldModelController,
                        Path = new PropertyPath(nameof(textFieldModelController.Data)),
                        Mode=BindingMode.TwoWay
                    };
                    tb.SetBinding(TextBox.TextProperty, sourceBinding);

                }
                else if (fieldModelController is NumberFieldModelController)
                {
                    tb = new TextBlock();
                    var numFieldModelController = fieldModelController as NumberFieldModelController;
                    Debug.Assert(numFieldModelController != null);
                    // make text update when changed
                    var sourceBinding = new Binding
                    {
                        Source = numFieldModelController,
                        Path = new PropertyPath(nameof(numFieldModelController.Data))
                    };
                    tb.SetBinding(TextBlock.TextProperty, sourceBinding);
                }

                // bind the text height
                var heightController = GetHeightFieldController(docController);
                BindHeight(tb, heightController);

                // bind the text width
                var widthController = GetWidthFieldController(docController);
                BindWidth(tb, widthController);

                // bind the text position
                var translateController = GetTranslateFieldController(docController);
                BindTranslation(tb, translateController);

                // add bindings to work with operators
                BindOperationInteractions(retToText, tb);

                return new List<FrameworkElement> { tb };
            }
        }

        /// <summary>
        /// A generic document type containing a single image.
        /// </summary>
        public class ImageBox : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");
            public static Key OpacityKey = new Key("78DB67E4-4D9F-47FA-980D-B8EEE87C4351", "Opacity Key");
            public static double OpacityDefault = 1;

            public ImageBox(FieldModel refToImage, double x=0, double y=0, double w=200, double h=200)
            {
                var fields = DefaultLayoutFields(x, y, w, h, refToImage);
                fields[OpacityKey] = new NumberFieldModel(OpacityDefault);
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();

                SetLayoutForDocument(Document, Document.DocumentModel);
            }
            public DocumentController MakeDelegate(ReferenceFieldModel refModel)
            {
                ContentController.AddModel(refModel);
                var fmc = new ReferenceFieldModelController(refModel);
                ContentController.AddController(fmc);
                var delg = Document.MakeDelegate();
                delg.SetField(DashConstants.KeyStore.DataKey, fmc, true);
                return delg;
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                // use the reference to the image to get the image field model controller
                var refToImage = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(refToImage != null);
                var imFieldModelController = ContentController.DereferenceToRootFieldModel<ImageFieldModelController>(refToImage);
                Debug.Assert(imFieldModelController != null);

                // the image field model controller provides us with the DATA
                // the Document on this courtesty document provides us with the parameters to display the DATA.
                // X, Y, Width, and Height etc....

                // create the image
                var image = new Image
                {
                    Stretch = Stretch.Fill // set image to fill container but ignore aspect ratio :/
                };

                // make image source update when changed
                var sourceBinding = new Binding
                {
                    Source = imFieldModelController,
                    Path = new PropertyPath(nameof(imFieldModelController.Data))
                };
                image.SetBinding(Image.SourceProperty, sourceBinding);

                // make image height resize
                var heightController = GetHeightFieldController(docController);
                BindHeight(image, heightController);

                // make image width resize
                var widthController = GetWidthFieldController(docController);
                BindWidth(image, widthController);

                // make image translate
                var translateController = GetTranslateFieldController(docController);
                BindTranslation(image, translateController);

                // set up interactions with operations
                BindOperationInteractions(refToImage, image);

                // make image opacity change
                var opacityController =
                    docController.GetField(OpacityKey) as NumberFieldModelController;
                Debug.Assert(opacityController != null);
                var opacityBinding = new Binding
                {
                    Source = opacityController,
                    Path = new PropertyPath(nameof(opacityController.Data))
                };
                image.SetBinding(UIElement.OpacityProperty, opacityBinding);

                return new List<FrameworkElement> { image };
            }

            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return ImageBox.MakeView(docController);
            }
        }

        /// <summary>
        /// A generic data wrappe document display type used to display images or text fields.
        /// </summary>
        public class DataBox : CourtesyDocument
        {
            CourtesyDocument _doc;
            public DataBox(ReferenceFieldModel refToField, bool isImage)
            {
                if (isImage)
                    _doc = new ImageBox(refToField);
                else
                    _doc = new TextingBox(refToField);
            }
            public override DocumentController Document { get { return _doc.Document; } set { _doc.Document = value; } }
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return _doc.makeView(docController);
            }
        }

        public class GenericCollection : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Generic Collection");

            void Initialize(FieldModel fieldModel)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, fieldModel);
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();

                SetLayoutForDocument(Document, Document.DocumentModel);
            }
            public GenericCollection(ReferenceFieldModel refToCollection) { Initialize(refToCollection); }
            public GenericCollection(DocumentCollectionFieldModel docCollection) { Initialize(docCollection); }
            
            static public List<FrameworkElement> MakeView(DocumentController docController)
            {
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                if (data != null)
                {
                    var w = docController.GetField(DashConstants.KeyStore.WidthFieldKey) != null ?
                        (docController.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController).Data : double.NaN;
                    var h = double.NaN;

                    var collectionFieldModelController = ContentController.DereferenceToRootFieldModel<DocumentCollectionFieldModelController>(data);
                    Debug.Assert(collectionFieldModelController != null);
                    var collectionModel = new CollectionModel(collectionFieldModelController.DocumentCollectionFieldModel, docController);
                    var collectionViewModel = new CollectionViewModel(collectionModel);
                    var view = new CollectionView(collectionViewModel);

                    var translateBinding = new Binding
                    {
                        Source = collectionFieldModelController,
                        Path = new PropertyPath("Pos"),
                        Mode = BindingMode.TwoWay,
                        Converter = new PointToTranslateTransformConverter()
                    };
                    view.SetBinding(UIElement.RenderTransformProperty, translateBinding);
                    if (w > 0)
                        view.Width = w;

                    return new List<FrameworkElement> { view };
                }
                return new List<FrameworkElement>();
            }
        }

        public class FreeformDocument : CourtesyDocument
        {
            public static DocumentType FreeFormDocumentType = new DocumentType("59B0C184-59BD-4570-87B8-0B660A68CBEC", "FreeFormDocument");

            public static DocumentType DocumentType { get { return FreeFormDocumentType; } }

            public FreeformDocument(IEnumerable<DocumentModel> docs)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModel(docs));
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, FreeFormDocumentType)).GetReturnedDocumentController();
            }

            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var output = new List<FrameworkElement>();

                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                var layoutData = ContentController.DereferenceToRootFieldModel<DocumentCollectionFieldModelController>(data);
                Debug.Assert(layoutData != null);

                foreach (var layoutDoc in layoutData.GetDocuments())
                {
                    var position =
                        (layoutDoc.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController)?.Data;
                    Debug.Assert(position != null);
                    var ele = layoutDoc.MakeViewUI();
                    foreach (var frameworkElement in ele)
                    {
                        frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
                        frameworkElement.VerticalAlignment = VerticalAlignment.Top;
                        frameworkElement.RenderTransform =
                            PointToTranslateTransformConverter.Instance.ConvertDataToXaml(position.Value);
                    }
                    output.AddRange(ele);
                }
                return output;
            }
        }

        /// <summary>
        /// Constructs a nested stackpanel that displays the fields of all documents in the list
        /// docs.
        /// </summary>
        public class StackingPanel : CourtesyDocument
        {
            public static DocumentType StackPanelDocumentType = new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Panel");

            static public DocumentType DocumentType { get { return StackPanelDocumentType; } }

            public StackingPanel(IEnumerable<DocumentModel> docs)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, new DocumentCollectionFieldModel(docs));
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, StackPanelDocumentType)).GetReturnedDocumentController();
            }

            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var stack = new StackPanel();
                stack.Orientation = Orientation.Horizontal;

                var stackFieldData = docController.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;

                if (stackFieldData != null)
                    foreach (var stackDoc in stackFieldData.GetDocuments())
                    {
                        foreach (var ele in stackDoc.MakeViewUI().Where((e) => e != null))
                        {
                            stack.Children.Add(ele);
                        }
                    }
                return new List<FrameworkElement>(new FrameworkElement[] { stack });
            }
        }

        public class PostitNote : CourtesyDocument
        {
            public static DocumentType PostitNoteType = new DocumentType("A5FEFB00-EA2C-4B64-9230-BBA41BACCAFC", "Post It");
            public static Key NotesFieldKey = new Key("A5486740-8AD2-4A35-A179-6FF1DA4D504F", "Notes");
            static DocumentController _prototypeLayout = CreatePrototypeLayout();
            static TextingBox _prototypeTextLayout;

            static DocumentController CreatePrototypeLayout()
            {
                _prototypeTextLayout = new TextingBox(new TextFieldModel("Text"), 0, 0, double.NaN, double.NaN);

                return _prototypeTextLayout.Document;
            }
            public PostitNote()
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(NotesFieldKey, new TextFieldModel("<your note>"));

                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, PostitNoteType)).GetReturnedDocumentController();
                
                var tBox = _prototypeTextLayout.MakeDelegate(new ReferenceFieldModel(Document.GetId(), NotesFieldKey));
                SetLayoutForDocument(tBox, tBox.DocumentModel);
             
                SetLayoutForDocument(Document, tBox.DocumentModel);
            }

        }

        public class TwoImages : CourtesyDocument
        {
            public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
            public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
            public static Key Image2FieldKey = new Key("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            static DocumentController _prototypeLayout = CreatePrototypeLayout();
            static ImageBox           _prototypeImage1Layout, _prototypeImage2Layout;
            static TextingBox         _prototypeTextLayout;

            static DocumentController CreatePrototypeLayout()
            {
                // set the default layout parameters on prototypes of field layout documents
                // these prototypes will be overridden by delegates when an instance is created
                _prototypeImage1Layout = new ImageBox(new TextFieldModel("Image 1"), 0, 20, 200, 200);
                _prototypeImage2Layout = new ImageBox(new TextFieldModel("Image 2"), 0, 220, 200, 200);
                _prototypeTextLayout   = new TextingBox(new TextFieldModel("Text"), 0, 0, 200, 20);

                return new FreeformDocument(new[] { _prototypeTextLayout.Document.DocumentModel, _prototypeImage1Layout.Document.DocumentModel, _prototypeImage2Layout.Document.DocumentModel }).Document;
            }
            public TwoImages(bool displayFieldsAsDocuments)
            {
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(TextFieldKey,  new TextFieldModel("Hello World!"));
                fields.Add(Image1FieldKey, new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg")));
                fields.Add(Image2FieldKey, new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));

                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, TwoImagesType)).GetReturnedDocumentController();

                // create delegates for each prototype layout field and override the instance data (DataKey and LayoutKey)
                var imBox1 = _prototypeImage1Layout.MakeDelegate(new ReferenceFieldModel(Document.GetId(), Image1FieldKey));
                var imBox2 = _prototypeImage2Layout.MakeDelegate(new ReferenceFieldModel(Document.GetId(), Image2FieldKey));
                var tBox   = _prototypeTextLayout.MakeDelegate  (new ReferenceFieldModel(Document.GetId(), TextFieldKey));

                SetLayoutForDocument(imBox1, imBox1.DocumentModel);
                SetLayoutForDocument(imBox2, imBox2.DocumentModel);
                SetLayoutForDocument(tBox,   tBox.DocumentModel);

                if (displayFieldsAsDocuments)
                {
                    var documentFieldModel = new DocumentCollectionFieldModel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel });
                    var documentFieldModelController = new DocumentCollectionFieldModelController(documentFieldModel);
                    ContentController.AddModel(documentFieldModel);
                    ContentController.AddController(documentFieldModelController);
                    Document.SetField(DashConstants.KeyStore.DataKey, documentFieldModelController, true);

                    var genericCollection = new GenericCollection(documentFieldModel).Document;
                    genericCollection.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(new NumberFieldModel(800)), true);

                    SetLayoutForDocument(Document, genericCollection.DocumentModel);
                }
                else
                {
                    // create a delegate for the prototype layout document and override the list of layout display elements :
                    //   the list of display elements is a list of delegates of the prototype layout documents prototype layout field documents.
                    SetLayoutForDocument(Document, CreateDelegateLayout(_prototypeLayout, new DocumentModel[] { imBox1.DocumentModel, imBox2.DocumentModel, tBox.DocumentModel }).DocumentModel);
                }
            }

        }
        public class NestedDocExample : CourtesyDocument
        {
            public static DocumentType NestedDocExampleType = new DocumentType("700FAEE4-5520-4E5E-9AED-3C8C5C1BE58B", "Nested Doc Example");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            public static Key TextField2Key = new Key("B53F1453-4C52-4302-96A3-A6B40DA7D587", "TextField2");
            public static Key TwoImagesKey = new Key("4E5C2B62-905D-4952-891D-24AADE14CA80", "TowImagesField");

            public NestedDocExample(bool displayFieldsAsDocuments)
            {
                // create a document with two images
                var twoModel = new DocumentModelFieldModel(new TwoImages(displayFieldsAsDocuments).Document.DocumentModel);
                var tModel   = new TextFieldModel("Nesting");
                var tModel2  = new TextFieldModel("More Nesting");
                var fields   = new Dictionary<Key, FieldModel>
                {
                    [TextFieldKey] = tModel,
                    [TwoImagesKey] = twoModel,
                    [TextField2Key] = tModel2
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, NestedDocExampleType)).GetReturnedDocumentController();

                var tBox   = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextFieldKey)).Document;
                var imBox1 = twoModel.Data;
                var tBox2  = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextField2Key)).Document;

                var stackPan = new StackingPanel(new DocumentModel[] { tBox.DocumentModel, imBox1, tBox2.DocumentModel }).Document;

                SetLayoutForDocument(Document, stackPan.DocumentModel);
            }
        }

        public class Numbers : CourtesyDocument
        {
            public static DocumentType NumbersType = new DocumentType("8FC422AB-015E-4B72-A28B-16271808C888", "Numbers");
            public static Key Number1FieldKey = new Key("0D3B939F-1E74-4577-8ACC-0685111E451C", "Number1");
            public static Key Number2FieldKey = new Key("56162B53-B02D-4880-912F-9D66B5F1F15B", "Number2");
            public static Key Number3FieldKey = new Key("61C34393-7DF7-4F26-9FDF-E0B138532F39", "Number3");

            public Numbers()
            {
                // create a document with two images
                var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
                fields.Add(Number1FieldKey, new NumberFieldModel(789));
                fields.Add(Number2FieldKey, new NumberFieldModel(23));
                fields.Add(Number3FieldKey, new NumberFieldModel(8));
                
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, NumbersType)).GetReturnedDocumentController();

                var imBox1 = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number1FieldKey), 0, 0, 50, 20).Document;
                var imBox2 = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number2FieldKey), 0, 0, 50, 20).Document;
                var tBox   = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number3FieldKey), 0, 0, 50, 20).Document;

                var stackPan = new StackingPanel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel }).Document;

                SetLayoutForDocument(Document, stackPan.DocumentModel);
            }

        }

        // == API COURTESY DOCUMENTS ==


        /// <summary>
        /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
        /// </summary>
        public class ApiDocumentModel : CourtesyDocument {
            public static DocumentType DocumentType = new DocumentType("APIC9C82-F32C-4704-AF6B-E55AC805C84F", "Api Source Creator");
            public static Key UrlKey = new Key("APIURL82-F32C-4704-AF6B-E55AC805C84F", "URL");
            public static Key MethodKey = new Key("APIMET82-F32C-4704-AF6B-E55AC805C84F", "Method");
            public static Key HeadersKey = new Key("APISECNN-F32C-4704-AF6B-E55AC805C84F", "Headers");
            public static Key ParametersKey = new Key("APIPARNN-F32C-4704-AF6B-E55AC805C84F", "Parameter");

            public static Key AuthMethodKey = new Key("APIMETAU-F32C-4704-AF6B-E55AC805C84F", "Auth Method");
            public static Key AuthUrlKey = new Key("APIURLAU-F32C-4704-AF6B-E55AC805C84F", "Auth URL");
            public static Key AuthKey = new Key("APIKEYAU-F32C-4704-AF6B-E55AC805C84F", "Auth Key");
            public static Key AuthSecret = new Key("APISECAU-F32C-4704-AF6B-E55AC805C84F", "Auth Secret");
            public static Key AuthHeaders = new Key("APISECAU-F32C-4704-AF6B-E55AC805C84F", "Auth Header");
            public static Key AuthParameters = new Key("APIPARAU-F32C-4704-AF6B-E55AC805C84F", "Auth Parameter");

            public static Key KeyTextKey = new Key("KEYURL82-F32C-4704-AF6B-E55AC805C84F", "Key");
            public static Key ValueTextKey = new Key("KEYMET82-F32C-4704-AF6B-E55AC805C84F", "Value");
            public static Key RequiredKey = new Key("KEYSECNN-F32C-4704-AF6B-E55AC805C84F", "Required");
            public static Key DisplayKey = new Key("KEYPARNN-F32C-4704-AF6B-E55AC805C84F", "Display");

           // public static Key CollectionResultKey = new Key("APICOLLN-F32C-4704-AF6B-E55AC805C84F", "Collection Result");

            public ApiDocumentModel() {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel> {
                    [UrlKey] = new TextFieldModel(""),
                    [MethodKey] = new NumberFieldModel(0),
                    [AuthUrlKey] = new TextFieldModel(""),
                    [AuthMethodKey] = new NumberFieldModel(0),
                    [AuthSecret] = new TextFieldModel(""),
                    [AuthKey] = new TextFieldModel(""),
                    [ParametersKey] = new DocumentCollectionFieldModel(new List<DocumentModel>()),
                    [HeadersKey] = new DocumentCollectionFieldModel(new List<DocumentModel>()),
                    [AuthParameters] = new DocumentCollectionFieldModel(new List<DocumentModel>()),
                    [AuthHeaders] = new DocumentCollectionFieldModel(new List<DocumentModel>()),

                    // TODO: differentiating similar fields in different documents for operator view
                    [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModel(new List<DocumentModel>())
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }

            /// <summary>
            /// Generates a new document containing the parameter information and adds that document to
            /// the corresponding DocumentCollectionFieldModel representing that parameter's list (i.e. Header, AuthParameters).
            /// </summary>
            /// <returns>The newly generated document representing the newly added parameter.</returns>
            public static DocumentController addParameter(DocumentController docController, TextBox key, TextBox value, CheckBox display,
                CheckBox required, Key parameterCollectionKey, ApiSourceDisplay sourceDisplay) {
                Debug.Assert(docController.DocumentType == DocumentType);
                Debug.Assert(parameterCollectionKey == AuthParameters || parameterCollectionKey == AuthHeaders ||
                    parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

                // fetch parameter list to add to
                DocumentCollectionFieldModelController col = (DocumentCollectionFieldModelController)docController.Fields[parameterCollectionKey];

                double displayDouble = ((bool)display.IsChecked) ? 0 : 1;
                double requiredDouble = ((bool)required.IsChecked) ? 0 : 1;

                // generate new doc with information to add
                var fields = new Dictionary<Key, FieldModel> {
                    [ValueTextKey] = new TextFieldModel(key.Text),
                    [DisplayKey] = new NumberFieldModel(displayDouble),
                    [KeyTextKey] = new TextFieldModel(value.Text),
                    [RequiredKey] = new NumberFieldModel(requiredDouble),
                };

                // add to collection & return new document result
                var ret = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();

                // apply textbox bindings
                bindToTextBox(key, ret.Fields[KeyTextKey]);
                bindToTextBox(value, ret.Fields[ValueTextKey]);

                // apply checkbox bindings
                bindToCheckBox(display, ret.Fields[DisplayKey]);
                bindToCheckBox(required, ret.Fields[RequiredKey]);

                // get the property's type
                ApiPropertyType type = ApiPropertyType.Parameter;
                if (parameterCollectionKey == HeadersKey)
                    type = ApiPropertyType.Header;
                if (parameterCollectionKey == AuthHeaders)
                    type = ApiPropertyType.AuthHeader;
                if (parameterCollectionKey == AuthParameters)
                    type = ApiPropertyType.AuthParameter;

                // make new property in source view
                ApiProperty apiprop = new ApiProperty(key.Text, value.Text, type, ret, required.IsChecked.Value);
                sourceDisplay.addToListView(apiprop);
                Debug.WriteLine("here: " + key.Text);

                // bind source's fields to those of the editor (key, value)
                TextFieldModelController textFieldModelController = ret.Fields[KeyTextKey] as TextFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiprop.XKey.SetBinding(TextBlock.TextProperty, sourceBinding);
                bindToTextBox(apiprop.XValue, ret.Fields[ValueTextKey]);

                // bind source visibility to display checkbox which is bound to backend display field of param document
                var binding = new Binding {
                    Source = display,
                    Path = new PropertyPath(nameof(display.IsChecked)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new BoolToVisibilityConverter()
                };
                apiprop.SetBinding(ApiProperty.VisibilityProperty, binding);

                // bind ApiRequired property to the required checkbox
                var bindin = new Binding {
                    Source = display,
                    Path = new PropertyPath(nameof(required.IsChecked)),
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiprop.XRequired.SetBinding(CheckBox.IsCheckedProperty, bindin);


                col.AddDocument(ret);
                return ret;
            }

            /// <summary>
            /// Removes a parameter from a given list of parameter documents.
            /// </summary>
            public static void removeParameter(DocumentController docController, DocumentController docModelToRemove,
                Key parameterCollectionKey, ApiSourceDisplay sourceDisplay) {
                Debug.Assert(docController.DocumentType == DocumentType);
                Debug.Assert(parameterCollectionKey == AuthParameters || parameterCollectionKey == AuthHeaders ||
                    parameterCollectionKey == ParametersKey || parameterCollectionKey == HeadersKey);

                DocumentCollectionFieldModelController col = (DocumentCollectionFieldModelController)docController.Fields[parameterCollectionKey];
                col.RemoveDocument(docModelToRemove);

            }

            // inherited
            public override List<FrameworkElement> makeView(DocumentController docController) {
                return TextingBox.MakeView(docController);
            }

            /// <summary>
            /// Binds a textbox to a fieldModelController.
            /// </summary>
            private static void bindToTextBox(TextBox tb, FieldModelController field) {

                // bind URL
                TextFieldModelController textFieldModelController = field as TextFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                tb.SetBinding(TextBox.TextProperty, sourceBinding);

            }

            /// <summary>
            /// Binds a textbox to a fieldModelController.
            /// </summary>
            private static void bindToCheckBox(CheckBox cb, FieldModelController field) {

                // bind URL
                NumberFieldModelController textFieldModelController = field as NumberFieldModelController;
                var sourceBinding = new Binding {
                    Source = textFieldModelController,
                    Path = new PropertyPath(nameof(textFieldModelController.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new DoubleToBoolConverter()
                };
                cb.SetBinding(CheckBox.IsCheckedProperty, sourceBinding);
                textFieldModelController.Data = 1;
            }

            private static void makeBinding(ApiCreatorDisplay apiDisplay, DocumentController docController) {

                // set up text bindings
                bindToTextBox(apiDisplay.UrlTB, docController.Fields[UrlKey]);
                bindToTextBox(apiDisplay.AuthDisplay.UrlTB, docController.Fields[AuthUrlKey]);
                bindToTextBox(apiDisplay.AuthDisplay.KeyTB, docController.Fields[AuthKey]);
                // bindToTextBox(apiDisplay.AuthDisplay.SecretTB, docController.Fields[AuthSecret]);

                // bind drop down list
                NumberFieldModelController fmcontroller = docController.Fields[MethodKey] as NumberFieldModelController;
                var sourceBinding = new Binding {
                    Source = fmcontroller,
                    Path = new PropertyPath(nameof(fmcontroller.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                apiDisplay.RequestMethodCB.SetBinding(ComboBox.SelectedIndexProperty, sourceBinding);

            }

            public static void setResults(DocumentController docController, List<DocumentController> documents) {
                (docController.Fields[DocumentCollectionFieldModelController.CollectionKey] as DocumentCollectionFieldModelController).SetDocuments(documents);
            }

            public static List<FrameworkElement> MakeView(DocumentController docController) {
                ApiSourceDisplay sourceDisplay = new ApiSourceDisplay();
                ApiCreatorDisplay apiDisplay = new ApiCreatorDisplay(docController, sourceDisplay);
                makeBinding(apiDisplay, docController);

                // test bindings are working
                Debug.WriteLine((docController.Fields[UrlKey] as TextFieldModelController).Data);
                apiDisplay.UrlTB.Text = "https://itunes.apple.com/search";
                Debug.WriteLine((docController.Fields[UrlKey] as TextFieldModelController).Data);

                // generate collection view preview for results
                var resultView = docController.Fields[DocumentCollectionFieldModelController.CollectionKey] as DocumentCollectionFieldModelController;
                var ctr = new GenericCollection(docController.Fields[DocumentCollectionFieldModelController.CollectionKey].FieldModel as DocumentCollectionFieldModel);
                var elements = new List<FrameworkElement>() { apiDisplay, sourceDisplay };
                var moreElements = GenericCollection.MakeView(ctr.Document);

                moreElements[0].Margin = new Thickness(450, 0, 0, 0);
                elements.AddRange(moreElements);

                // this binding makes it s.t. either only the ApiSource or the ApiSourceCreator is visible at a single time
                // TODO: should clients be able to decide for themselves how this is displaying (separate superuser and regular user)
                // or should everyone just see the same view ?
                // bind URL
                var sourceBinding = new Binding {
                    Source = apiDisplay,
                    Path = new PropertyPath(nameof(apiDisplay.Visibility)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new InverseVisibilityConverter()
                };
                sourceDisplay.SetBinding(ApiSourceDisplay.VisibilityProperty, sourceBinding);

                // return all results
                return elements;
            }
        }

        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class DoubleToBoolConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, string language) {
                return ((double)value != 0);
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language) {
                if ((bool)value) return 1;
                return 0;
            }
        }


        /// <summary>
        /// Converts doubles to booleans and back. 0 = false, 1 = true (or any nonzero number). Used
        /// primarily to convert NumberFieldModels into boolean values.
        /// </summary>
        public class InverseVisibilityConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, string language) {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                return Windows.UI.Xaml.Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language) {
                if ((Windows.UI.Xaml.Visibility)value == Windows.UI.Xaml.Visibility.Collapsed)
                    return Windows.UI.Xaml.Visibility.Visible;
                else
                    return Windows.UI.Xaml.Visibility.Collapsed;
            }
        }


    }
}
