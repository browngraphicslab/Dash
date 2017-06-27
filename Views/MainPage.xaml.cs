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
            var collectionDocumentController  = new GenericCollection(new DocumentCollectionFieldModel(new List<DocumentModel>())).Document;
            // set the main view's datacontext to be the collection
            MainDocView.DataContext = new DocumentViewModel(collectionDocumentController);

            // set the main view's width and height to avoid NaN errors
            MainDocView.Width = MyGrid.ActualWidth;
            MainDocView.Height = MyGrid.ActualHeight;

            // TODO someone who understands this explain what it does
            MainDocView.ManipulationMode = ManipulationModes.None;
            MainDocView.Manipulator.RemoveAllButHandle();

            MainDocView.DraggerButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            // Set the instance to be itself, there should only ever be one MainView
            Debug.Assert(Instance == null, "If the main view isn't null then it's been instantiated multiple times and setting the instance is a problem");
            Instance = this;

            /* //TODO this seriously slows down the document 
            var jsonDoc = JsonToDashUtil.RunTests();
            DisplayDocument(jsonDoc);
            */ 
        }

        private void OnToggleEditMode(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //xFreeformView.ToggleEditMode();
        }

        private void OnOperatorAdd(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {

            //Create Operator document
            var opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorModel());
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
            //DocumentModel unionOpModel =
            //    OperatorDocumentModel.CreateOperatorDocumentModel(new UnionOperatorModel());
            //var unionOpCont = new DocumentController(unionOpModel);
            //docEndpoint.UpdateDocumentAsync(unionOpModel);
            //DocumentView unionView = new DocumentView
            //{
            //    Width = 200,
            //    Height = 200
            //};
            //DocumentViewModel unionOpvm = new DocumentViewModel(unionOpCont);
            //unionView.DataContext = unionOpvm;
            //DisplayDocument(unionOpCont);

            // add image url -> image operator for testing
            DocumentController imgOpModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(new ImageOperatorModel());
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

        public void DisplayDocument(DocumentController docModel, Point? where = null)
        {
            var children = MainDocument.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
            if (children != null)
            {
                children.AddDocument(docModel);

                //if (where.HasValue)
                //{
                //    docModel.SetField(DocumentModel.GetFieldKeyByName("X"), new NumberFieldModel(((Point)where).X), false);
                //    docModel.SetField(DocumentModel.GetFieldKeyByName("Y"), new NumberFieldModel(((Point)where).Y), false);
                //}
            }
        }

        private void AddCollection(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //DocumentModel image2 = DocumentModel.TwoImagesAndText();
            //DocumentModel image2Del = image2.MakeDelegate();
            //DocumentModel umpireDoc = DocumentModel.UmpireDocumentModel();
            //image2Del.SetField(DocumentModel.LayoutKey, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(image2Del.DocumentType, true)), true);
            //image2Del.SetField(DocumentModel.GetFieldKeyByName("content"), new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);

            //var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //if (docCollection == null)
            //{
            //    docCollection = docController.CreateDocumentAsync("newtype");
            //    docCollection.SetField(DocumentModel.GetFieldKeyByName("children"), new DocumentCollectionFieldModel(new DocumentModel[] { image2, image2Del, umpireDoc }), false);
            //}
            //DisplayDocument(docCollection);
        }

        private void AddApiCreator(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            throw new NotImplementedException();

            // xFreeformView.Canvas.Children.Add(new Sources.Api.ApiCreatorDisplay());
        }

        private void AddImage(object sender, TappedRoutedEventArgs tappedRoutedEventArgs) {
            throw new NotImplementedException();
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.FilePickerDisplay());
            // xFreeformView.Canvas.Children.Add(new Sources.FilePicker.PDFFilePicker());
        }

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
        }

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
                OperatorView opView = new OperatorView {DataContext = rfm};
                return new List<FrameworkElement> {opView};
            }
        }

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
                    [DashConstants.KeyStore.DataKey] = refToText
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return TextingBox.MakeView(docController);
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var fw = docController.GetField(FontWeightKey);
                var fontWeight = fw is TextFieldModelController ? ((fw as TextFieldModelController).Data == "Bold" ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal) : Windows.UI.Text.FontWeights.Normal;
               
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                if (data != null)
                    return new TextTemplateModel(0,0,fontWeight).MakeViewUI(data, docController);
                return new List<FrameworkElement>();
            }
        }
        public class ImageBox : CourtesyDocument
        {
            public static DocumentType DocumentType = new DocumentType("3A6F92CC-D8DC-448B-9D3E-A1E04C2C77B3", "Image Box");
            public ImageBox(ReferenceFieldModel refToImage)
            {
                // create a layout for the image
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = refToImage,
                    [DashConstants.KeyStore.WidthFieldKey] = new NumberFieldModel(double.NaN),
                    [DashConstants.KeyStore.HeightFieldKey] = new NumberFieldModel(double.NaN)
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }
            public static List<FrameworkElement> MakeView(DocumentController docController)
            {
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                if (data != null)
                    return new ImageTemplateModel(0, 0).MakeViewUI(data, docController);
                return new List<FrameworkElement>();
            }
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return ImageBox.MakeView(docController);
            }
        }

        public class DataBox: CourtesyDocument
        {
            CourtesyDocument _doc;
            public DataBox(ReferenceFieldModel refToImage, bool isImage)
            {
                _doc = isImage ? (CourtesyDocument)new ImageBox(refToImage) : new TextingBox(refToImage);
            }
            public override DocumentController Document { get { return _doc.Document; } set { _doc.Document = value; } }
            public override List<FrameworkElement> makeView(DocumentController docController)
            {
                return _doc.makeView(docController);
            }
        }

        public class GenericCollection : CourtesyDocument {
            public static DocumentType DocumentType = new DocumentType("7C59D0E9-11E8-4F12-B355-20035B3AC359", "Generic Collection");

            void Initialize(FieldModel fieldModel)
            {
                var fields = new Dictionary<Key, FieldModel>
                {
                    [DashConstants.KeyStore.DataKey] = fieldModel
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, DocumentType)).GetReturnedDocumentController();
            }
            public GenericCollection(ReferenceFieldModel refToCollection) { Initialize(refToCollection); }
            public GenericCollection(DocumentCollectionFieldModel docCollection) { Initialize(docCollection); }
          
            static public List<FrameworkElement> MakeView(DocumentController docController)
            {
                var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
                if (data != null)
                    return new DocumentCollectionTemplateModel(0, 0).MakeViewUI(data, docController);
                return new List<FrameworkElement>();
            }
        }


        public class StackingPanel : CourtesyDocument
        {
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
                    foreach (var stackDoc in stackFieldData.Documents)
                    {
                        foreach (var ele in stackDoc.MakeViewUI().Where((e) => e!= null))
                        {
                            if (double.IsNaN(ele.Width))
                                ele.MaxWidth = 300;
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

            public TwoImages()
            {
                // create a document with two images
                var imModel = new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"));
                var tModel = new TextFieldModel("Hello World!");
                var fields = new Dictionary<Key, FieldModel>
                {
                    [TextFieldKey] = tModel,
                    [Image1FieldKey] = imModel,
                    [Image2FieldKey] = imModel
                };
                Document = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, TwoImagesType)).GetReturnedDocumentController();

                var imBox1 = new ImageBox(new ReferenceFieldModel(Document.GetId(), Image1FieldKey)).Document;
                var imBox2 = new ImageBox(new ReferenceFieldModel(Document.GetId(), Image2FieldKey)).Document;
                var tBox   = new TextingBox(new ReferenceFieldModel(Document.GetId(), TextFieldKey)).Document;

                var stackPan = new StackingPanel(new DocumentModel[] { tBox.DocumentModel, imBox1.DocumentModel, imBox2.DocumentModel }).Document;

                //SetLayoutForDocument(stackPan.DocumentModel);
            }
            
        }

        private async void AddDocuments(object sender, TappedRoutedEventArgs e)
        {
            DisplayDocument(new TwoImages().Document);
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
    }
}
