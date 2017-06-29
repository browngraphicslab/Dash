using Dash.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Models.OperatorModels.Set;
using Dash.ViewModels;
using DashShared;
using Dash.Sources.Api;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Dash
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {

        static public MainPage Instance;

        public MainPage()
        {
            this.InitializeComponent();

            // adds items from the overlay canvas onto the freeform canvas
            xOverlayCanvas.OnAddDocumentsTapped += AddDocuments;
            xOverlayCanvas.OnAddCollectionTapped += AddCollection;
            xOverlayCanvas.OnAddAPICreatorTapped += AddApiCreator;
            xOverlayCanvas.OnAddImageTapped += AddImage;
            xOverlayCanvas.OnAddShapeTapped += AddShape;
            xOverlayCanvas.OnOperatorAdd += OnOperatorAdd;
            xOverlayCanvas.OnToggleEditMode += OnToggleEditMode;

            // create the collection document model using a request
            var collectionDocumentController = new GenericCollection(new DocumentCollectionFieldModel(new List<DocumentModel>())).Document;
            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(collectionDocumentController)
            {
                IsDetailedUserInterfaceVisible = false,
                IsMoveable = false
            };

            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;
            
            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            //TODO this seriously slows down the document 
            var jsonDoc = JsonToDashUtil.RunTests();
            DisplayDocument(jsonDoc);
        }

        private void OnToggleEditMode(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //xFreeformView.ToggleEditMode();
        }


        private void OnOperatorAdd(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //Create Operator document
            var opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController(new OperatorFieldModel("Divide")));
            DocumentView view = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel opvm = new DocumentViewModel(opModel);
            //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
            view.DataContext = opvm;

            DisplayDocument(opModel);


            //xFreeformView.AddOperatorView(opvm, view, 50, 50);

            //// add union operator for testing 
            DocumentController intersectOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new IntersectionOperatorModelController(new OperatorFieldModel("Intersection")));
            DocumentView intersectView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel intersectOpvm = new DocumentViewModel(intersectOpModel);
            intersectView.DataContext = intersectOpvm;
            DisplayDocument(intersectOpModel);

            DocumentController unionOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new UnionOperatorFieldModelController(new OperatorFieldModel("Union")));
            DocumentView unionView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel unionOpvm = new DocumentViewModel(unionOpModel);
            unionView.DataContext = unionOpvm;
            DisplayDocument(unionOpModel);

            // add image url -> image operator for testing
            DocumentController imgOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new ImageOperatorFieldModelController(new OperatorFieldModel("ImageToUri")));
            DocumentView imgOpView = new DocumentView
            {
                Width = 200,
                Height = 200
            };
            DocumentViewModel imgOpvm = new DocumentViewModel(imgOpModel);
            imgOpView.DataContext = imgOpvm;
            DisplayDocument(imgOpModel);
        }

        private async void AddShape(object sender, TappedRoutedEventArgs e)
        {
            var shapeModel = new ShapeModel
            {
                Width = 300,
                Height = 300,
                X = 300,
                Y = 300,
                Id = $"{Guid.NewGuid()}"
            };

            var shapeEndpoint = App.Instance.Container.GetRequiredService<ShapeEndpoint>();
            var result = await shapeEndpoint.CreateNewShape(shapeModel);
            if (result.IsSuccess)
            {
                shapeModel = result.Content;
            }
            else
            {
                Debug.WriteLine(result.ErrorMessage);
                return;
            }

            throw new NotImplementedException();
            //var shapeController = new ShapeController(shapeModel);
            //throw new NotImplementedException("The shape controller has not been updated to work with controllers");
            ////ContentController.AddShapeController(shapeController);

            //var shapeVM = new ShapeViewModel(shapeController);
            //var shapeView = new ShapeView(shapeVM);


            //  xFreeformView.Canvas.Children.Add(shapeView);
        }


        public DocumentController MainDocument => (MainDocView.DataContext as DocumentViewModel)?.DocumentController;

        /// <summary>
        /// Adds new documents to the MainView document. New documents are added as children of the Main document.
        /// </summary>
        /// <param name="docModel"></param>
        /// <param name="where"></param>
        public void DisplayDocument(DocumentController docModel, Point? where = null)
        {
            var children = MainDocument.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
            if (children != null)
                children.AddDocument(docModel);
        }

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            var twoImages = new TwoImages(false).Document;
            var twoImages2 = new TwoImages(false).Document;
            var numbers = new Numbers(1,1,1).Document;

            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>
            {
                {DocumentCollectionFieldModelController.CollectionKey, new DocumentCollectionFieldModel(new DocumentModel[] {numbers.DocumentModel}) }
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("collection", "collection"))).GetReturnedDocumentController();
            var layoutDoc = new GenericCollection(new ReferenceFieldModel(col.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            var documentFieldModel = new DocumentModelFieldModel(layoutDoc.DocumentModel);
            var layoutController = new DocumentFieldModelController(documentFieldModel);
            ContentController.AddModel(documentFieldModel);
            ContentController.AddController(layoutController);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);

            AddAnotherLol();
        }

        private void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            DisplayDocument(new ApiSourceCreatorDoc().Document);
        }

        private void AddAnotherLol()
        {
            // collection no.2
            var numbers = new Numbers(2,2,2).Document;
            var numbers2 = new Numbers(3,3,3).Document;
            var twoImages2 = new TwoImages(false).Document;

            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>
            {
                {
                    DocumentCollectionFieldModelController.CollectionKey,
                    new DocumentCollectionFieldModel(
                                                        new DocumentModel[] 
                                                        {
                                                            
                                                            numbers.DocumentModel,
                                                            numbers2.DocumentModel
                                                        }
                                                    )
                }
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("collection", "collection"))).GetReturnedDocumentController();
            var layoutDoc = new GenericCollection(new ReferenceFieldModel(col.GetId(), DocumentCollectionFieldModelController.CollectionKey)).Document;
            var documentFieldModel = new DocumentModelFieldModel(layoutDoc.DocumentModel);
            var layoutController = new DocumentFieldModelController(documentFieldModel);
            ContentController.AddModel(documentFieldModel);
            ContentController.AddController(layoutController);
            col.SetField(DashConstants.KeyStore.LayoutKey, layoutController, true);
            DisplayDocument(col);
        }
        

        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            throw new NotImplementedException();
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

        /// <summary>
        /// This class provides base functionality for creating and displaying new documents.
        /// </summary>
        public class CourtesyDocument
        {
            public virtual DocumentController Document { get; set; }
            public void SetLayoutForDocument(DocumentModel layoutDoc)
            {
                var documentFieldModel = new DocumentModelFieldModel(layoutDoc);
                var layoutController = new DocumentFieldModelController(documentFieldModel);
                ContentController.AddModel(documentFieldModel);
                ContentController.AddController(layoutController);
                Document.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);
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
                renderElement.SetBinding(HeightProperty, heightBinding);
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
                renderElement.SetBinding(WidthProperty, widthBinding);
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
                renderElement.SetBinding(RenderTransformProperty, translateBinding);
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
        /// Given a reference to an operator field model, constructs a document type that displays that operator.
        /// </summary>
        public class OperatorBox : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

            public OperatorBox(ReferenceFieldModel refToOp)
            {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = refToOp
                };
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
        /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
        /// </summary>
        public class ApiSourceCreatorDoc : CourtesyDocument {
            public static DocumentType DocumentType = new DocumentType("APIC9C82-F32C-4704-AF6B-E55AC805C84F", "Api Source Creator");

            public ApiSourceCreatorDoc() {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel> {
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }

            public override List<FrameworkElement> makeView(DocumentController docController) {
                return TextingBox.MakeView(docController);
            }
            public static List<FrameworkElement> MakeView(DocumentController docController) {
                return new List<FrameworkElement>() { new ApiCreatorDisplay() };
            }
        }


        /// <summary>
        /// Wrapper document to display the ApiSourceCreatorDisplay Usercontrol.
        /// </summary>
        public class ApiSourceDoc : CourtesyDocument {
            public static DocumentType DocumentType = new DocumentType("66FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");
            public static Key ApiFieldKey = new Key("927F581B-6ECB-49E6-8EB3-B8949DE0FE21", "Api");
            private static ApiSourceDisplay source;

            public ApiSourceDoc(ApiSourceDisplay source) {
                // create a layout for the image
                ApiSourceDoc.source = source;
                var fields = new Dictionary<Key, FieldModel> {
            };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }

            public override List<FrameworkElement> makeView(DocumentController docController) {
                return new List<FrameworkElement>() { source };
            }

            public static List<FrameworkElement> MakeView(DocumentController docController) {
                return new List<FrameworkElement>() { ApiSourceDoc.source };
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

            public TextingBox(ReferenceFieldModel refToText)
            {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = refToText,
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModel(200),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModel(30),
                    [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModel(0, 0)
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
                SetLayoutForDocument(Document.DocumentModel);
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
                var tb = new TextBlock();

                // use the reference to the text to get the text field model controller
                var retToText = docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
                Debug.Assert(retToText != null);
                var fieldModelController = ContentController.DereferenceToRootFieldModel(retToText);
                if (fieldModelController is TextFieldModelController)
                {
                    var textFieldModelController = fieldModelController as TextFieldModelController;
                    Debug.Assert(textFieldModelController != null);
                    // make text update when changed
                    var sourceBinding = new Binding
                    {
                        Source = textFieldModelController,
                        Path = new PropertyPath(nameof(textFieldModelController.Data))
                    };
                    tb.SetBinding(TextBlock.TextProperty, sourceBinding);

                } else if (fieldModelController is NumberFieldModelController)
                {
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
            public ImageBox(ReferenceFieldModel refToImage)
            {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = refToImage,
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModel(200),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModel(200),
                    [DashConstants.KeyStore.PositionFieldKey] = new PointFieldModel(0,0)
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();

                SetLayoutForDocument(Document.DocumentModel);
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
        public class DataBox: CourtesyDocument
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
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = fieldModel
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
                
                SetLayoutForDocument(Document.DocumentModel);
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
                    return new DocumentCollectionTemplateModel(0, 0, w, h).MakeViewUI(data, docController);
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
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = new DocumentCollectionFieldModel(docs)
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, FreeFormDocumentType)).GetReturnedDocumentController();
            }

            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var output = new List<FrameworkElement>();

                var layoutData = docController.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
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
        
        public class StackingPanel : CourtesyDocument {
            public static DocumentType StackPanelDocumentType = new DocumentType("61369301-820F-4779-8F8C-701BCB7B0CB7", "Stack Panel");

            static public DocumentType DocumentType { get { return StackPanelDocumentType;  } }

            public StackingPanel(IEnumerable<DocumentModel> docs)
            {
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = new DocumentCollectionFieldModel(docs)
                };
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


        public class TwoImages : CourtesyDocument
        {
            public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
            public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
            public static Key Image2FieldKey = new Key("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
            public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");

            public TwoImages(bool displayFieldsAsDocuments)
            {
                // create a document with two images
                var imModel  = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat.jpg"));
                var imModel2 = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
                var tModel   = new TextFieldModel("Hello World!");
                var fields   = new Dictionary<Key, FieldModel>
                {
                    [TextFieldKey]   = tModel,
                    [Image1FieldKey] = imModel,
                    [Image2FieldKey] = imModel2
                };

                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, TwoImagesType)).GetReturnedDocumentController();

               
                var imBox1 = new ImageBox(new ReferenceFieldModel(Document.GetId(), Image1FieldKey)).Document;
                var imBox2 = new ImageBox(new ReferenceFieldModel(Document.GetId(), Image2FieldKey)).Document;
                var tBox = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextFieldKey)).Document;

                if (displayFieldsAsDocuments)
                {
                    var documentFieldModel = new DocumentCollectionFieldModel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel } );
                    var documentFieldModelController = new DocumentCollectionFieldModelController(documentFieldModel);
                    ContentController.AddModel(documentFieldModel);
                    ContentController.AddController(documentFieldModelController);
                    Document.SetField(DashConstants.KeyStore.DataKey, documentFieldModelController, true);

                    var genericCollection = new GenericCollection(documentFieldModel).Document;
                    genericCollection.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(new NumberFieldModel(800)), true);

                    SetLayoutForDocument(genericCollection.DocumentModel);
                } else
                {
                    var freeFormDoc = new FreeformDocument(new [] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel }).Document;

                    SetLayoutForDocument(freeFormDoc.DocumentModel);
                }
            }

        }
        public class NestedDocExample : CourtesyDocument
        {
            public static DocumentType NestedDocExampleType = new DocumentType("700FAEE4-5520-4E5E-9AED-3C8C5C1BE58B", "Nested Doc Example");
            public static Key TextFieldKey  = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
            public static Key TextField2Key = new Key("B53F1453-4C52-4302-96A3-A6B40DA7D587", "TextField2");
            public static Key TwoImagesKey  = new Key("4E5C2B62-905D-4952-891D-24AADE14CA80", "TowImagesField");

            public NestedDocExample(bool displayFieldsAsDocuments)
            {
                // create a document with two images
                var twoModel = new DocumentModelFieldModel(new TwoImages(displayFieldsAsDocuments).Document.DocumentModel);
                var tModel   = new TextFieldModel("Nesting");
                var tModel2 = new TextFieldModel("More Nesting");
                var fields   = new Dictionary<Key, FieldModel>
                {
                    [TextFieldKey]  = tModel,
                    [TwoImagesKey]  = twoModel,
                    [TextField2Key] = tModel2
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, NestedDocExampleType)).GetReturnedDocumentController();

                var tBox   = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextFieldKey)).Document;
                var imBox1 = twoModel.Data;
                var tBox2 = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextField2Key)).Document;

                var stackPan = new StackingPanel(new DocumentModel[] { tBox.DocumentModel, imBox1, tBox2.DocumentModel }).Document;

                SetLayoutForDocument(stackPan.DocumentModel);
            }
        }

        public class Numbers : CourtesyDocument
        {
            public static DocumentType NumbersType = new DocumentType("8FC422AB-015E-4B72-A28B-16271808C888", "Numbers");
            public static Key Number1FieldKey = new Key("0D3B939F-1E74-4577-8ACC-0685111E451C", "Number1");
            public static Key Number2FieldKey = new Key("56162B53-B02D-4880-912F-9D66B5F1F15B", "Number2");
            public static Key Number3FieldKey = new Key("61C34393-7DF7-4F26-9FDF-E0B138532F39", "Number3");

            public Numbers(int num1, int num2, int num3)
            {
                // create a document with two images
                var fields = new Dictionary<Key, FieldModel>
                {
                    [Number1FieldKey] = new NumberFieldModel(num1),
                    [Number2FieldKey] = new NumberFieldModel(num2),
                    [Number3FieldKey] = new NumberFieldModel(num3)
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, NumbersType)).GetReturnedDocumentController();

                var imBox1 = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number1FieldKey)).Document;
                var imBox2 = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number2FieldKey)).Document;
                var tBox = new TextingBox(new ReferenceFieldModel(Document.GetId(), Number3FieldKey)).Document;

                var stackPan = new StackingPanel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel }).Document;
            }

        }
        
        private void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DisplayDocument(new TwoImages(false).Document);
            DisplayDocument(new Numbers(234,234,234).Document);
            //DisplayDocument(new NestedDocExample(true).Document);
            //DisplayDocument(new NestedDocExample(false).Document);
        }


        private void MyGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var child = xViewBox.Child as FrameworkElement;
            if (child != null)
            {
                child.Width = e.NewSize.Width;
                child.Height = e.NewSize.Height;
            }
        }

        // FILE DRAG AND DROP

        /// <summary>
        /// Handles drop events onto the canvas, usually by creating a copy document of the original and
        /// placing it into the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">drag event arguments</param>
        private async void XCanvas_Drop(object sender, DragEventArgs e)
        {
            Image dragged = new Image();
            string url = "";

            // load items dragged from solution explorer
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    // parse images dropped in
                    if (contentType == "image/jpg" || contentType == "image/png" || contentType == "image/jpeg")
                    {
                        StorageFile newFile = await storageFile.CopyAsync(folder, storageFile.Name, NameCollisionOption.GenerateUniqueName);
                        url = newFile.Path;
                        BitmapImage bitmapImg = new BitmapImage();

                        bitmapImg.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                        dragged.Source = bitmapImg;
                    }

                    // parse text files dropped in
                    if (contentType == "text/plain")
                    {
                        // TODO: TEXT FILES
                        return;
                    }
                }
            }

            if (e.DataView.Properties["image"] != null)
                dragged = e.DataView.Properties["image"] as Image; // fetches stored drag object

            // make document
            // generate single-image document model
            ImageFieldModel m = new ImageFieldModel(new Uri(url));
            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>
            {
                [new Key("DRAGIMGF-1E74-4577-8ACC-0685111E451C", "image")] = m
            };

            var col = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, new DocumentType("dragimage", "dragimage"))).GetReturnedDocumentController();
            DisplayDocument(col);
        }

        public void XCanvas_DragOver_1(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}
