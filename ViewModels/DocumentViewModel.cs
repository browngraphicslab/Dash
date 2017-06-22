using Dash;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Dash.Models;
using Windows.Foundation;
using System.Diagnostics;

namespace Dash
{
    public class DocumentViewModel : ViewModelBase
    {
        // == MEMBERS, GETTERS, SETTERS ==
        static DocumentModel DefaultLayoutModelSource = null;
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private double _x, _y;
        private Brush _backgroundBrush;
        private Brush _borderBrush;
        public bool DoubleTapEnabled = true;

        public double Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value); }
        }

        public double Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value); }
        }
        public double X
        {
            get { return _x; }
            set { SetProperty(ref _x, value); }
        }

        public double Y
        {
            get { return _y; }
            set { SetProperty(ref _y, value); }
        }


        public ManipulationModes ManipulationMode
        {
            get { return _manipulationMode; }
            set { SetProperty(ref _manipulationMode, value); }
        }

        public Brush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set { SetProperty(ref _backgroundBrush, value); }
        }

        public Brush BorderBrush
        {
            get { return _borderBrush; }
            set { SetProperty(ref _borderBrush, value); }
        }
        public DocumentModel DocumentModel { get; set; }

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

        public DocumentViewModel(DocumentModel docModel)
        {
            DocumentModel = docModel;
            DocumentModel.DocumentFieldUpdated -= DocumentModel_DocumentFieldUpdated;
            DocumentModel.DocumentFieldUpdated += DocumentModel_DocumentFieldUpdated;
            if (docModel.DocumentType.Type == "collection_example")
            {
                DoubleTapEnabled = false;
                BackgroundBrush = new SolidColorBrush(Colors.Transparent);
                BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                BackgroundBrush = new SolidColorBrush(Colors.White);
                BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
            }
            if (docModel.Field(DocumentModel.GetFieldKeyByName("X")) != null &&
                docModel.Field(DocumentModel.GetFieldKeyByName("Y")) != null)
            {
                X = (docModel.Field(DocumentModel.GetFieldKeyByName("X")) as NumberFieldModel).Data;
                Y = (docModel.Field(DocumentModel.GetFieldKeyByName("Y")) as NumberFieldModel).Data;
            }
        }

        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            if (fieldReference.FieldKey == DocumentModel.GetFieldKeyByName("X"))
                X = (DocumentModel.Field(DocumentModel.GetFieldKeyByName("X")) as NumberFieldModel).Data;
            if (fieldReference.FieldKey == DocumentModel.GetFieldKeyByName("Y"))
                Y = (DocumentModel.Field(DocumentModel.GetFieldKeyByName("Y")) as NumberFieldModel).Data;
        }

        // == METHODS ==
        /// <summary>
        /// Generates a list of UIElements by making FieldViewModels of a document;s
        /// given fields.
        /// </summary>
        /// TODO: rename this to create ui elements
        /// <returns>List of all UIElements generated</returns>
        public virtual List<UIElement> GetUiElements(Rect bounds)
        {
            var uiElements = new List<UIElement>();
            var layout = GetLayoutModel();

            var size = new Size();
            if (layout.ShowAllFields) 
            {
                size = showAllDocumentFields(uiElements, bounds);
            }
            else
            {
                var transXf = new TranslateTransform();
                transXf.X = bounds.Left;
                transXf.Y = bounds.Top;
                foreach (var lEle in layout.Fields)
                {
                    var uiele = lEle.Value.MakeViewUI(DocumentModel.Field(lEle.Key), DocumentModel);
                    if (uiele != null)
                    {
                        uiElements.AddRange(uiele);
                        size = new Size(Math.Max(size.Width, lEle.Value.Left + lEle.Value.Width), Math.Max(size.Height, lEle.Value.Top+lEle.Value.Height));
                    }
                }
            }
            if (bounds.Height > 0 && size.Height > 0 && bounds.Width > 0 && size.Width > 0)
            {
                double scaling = Math.Min(bounds.Width / size.Width, bounds.Height / size.Height);
                var transXf = new TranslateTransform();
                transXf.X = bounds.Left;
                transXf.Y = bounds.Top;
                var scaleXf = new ScaleTransform();
                scaleXf.ScaleX = scaling;
                scaleXf.ScaleY = scaling;
                foreach (var ui in uiElements)
                {
                    var xfg = new TransformGroup();
                    xfg.Children.Add(ui.RenderTransform);
                    xfg.Children.Add(scaleXf);
                    xfg.Children.Add(transXf);

                    ui.RenderTransform = xfg;
                }
            }
            return uiElements;
        }

        Size showAllDocumentFields(List<UIElement> uiElements, Rect bounds)
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();

            double yloc = bounds.Height > 0 ? 0 : bounds.Top;
            foreach (var f in DocumentModel.EnumFields(true))
                if (f.Key != DocumentModel.DelegatesKey)
                {
                    var fieldModel = f.Value;
                    while (fieldModel is ReferenceFieldModel)
                    {
                        fieldModel = docController.GetDocumentAsync((fieldModel as ReferenceFieldModel).DocId).Field((fieldModel as ReferenceFieldModel).FieldKey);
                    }
                    if (fieldModel is DocumentCollectionFieldModel)
                    {
                        uiElements.AddRange(new DocumentCollectionTemplateModel(bounds.Left, yloc, 500, 100, Visibility.Visible).MakeViewUI(fieldModel, DocumentModel));
                        yloc += 100;
                    }
                    else if (fieldModel is ImageFieldModel || (fieldModel is TextFieldModel && (fieldModel as TextFieldModel).Data.EndsWith(".jpg")))
                    {
                        uiElements.AddRange(new ImageTemplateModel(bounds.Left, yloc, 500, 500).MakeViewUI(fieldModel, DocumentModel));
                        yloc += 500;
                    }
                    else if (fieldModel != null)
                    {
                        uiElements.AddRange(new TextTemplateModel(bounds.Left, yloc, FontWeights.Bold, TextWrapping.Wrap, Visibility.Visible).MakeViewUI(fieldModel, DocumentModel));
                        yloc += 20;
                    }
                }
            return new Size(0, yloc);
        }
        
        public LayoutModel GetLayoutModel()
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);

            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var refField = docController.GetDocumentAsync(layoutModelRef.DocId).Field(layoutModelRef.FieldKey) as LayoutModelFieldModel;

            return refField.Data;
        }

        public void SetLayoutModel(LayoutModel layoutModel)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);
        }

        static Key GetFieldKeyByName(string name)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var key = keyController.GetKeyAsync(name);
            if (key == null)
                key = keyController.CreateKeyAsync(name);
            return key;
        }

        /// <summary>
        /// find the layoutModel to use to display this document and return it as referenceField to where the layoutModel is stored.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        static ReferenceFieldModel GetLayoutModelReferenceForDoc(DocumentModel doc)
        {
            var layoutField = doc.Field(DocumentModel.LayoutKey);

            // If the Layout field is a LayoutModel, then use it.
            if (layoutField is LayoutModelFieldModel)
            {
                return new ReferenceFieldModel(doc.Id, DocumentModel.LayoutKey);
            }

            // otherwise lookup a LayoutModel for doc's type on a specified settings document or the default settings document
            var settingsDocument = layoutField is DocumentModelFieldModel ? (layoutField as DocumentModelFieldModel).Data : DefaultLayoutModelSource;
            return getLayoutModelReferenceForDocumentType(doc.DocumentType, settingsDocument);
        }

        static ReferenceFieldModel getLayoutModelReferenceForDocumentType(DocumentType docType,DocumentModel layoutModelSource)
        {
            //effectively, this sets defaultlayoutmodelsource if it hasnt been instantiated yet to a new doc each time
            if (layoutModelSource == null)
            {
                var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                layoutModelSource = DefaultLayoutModelSource = docController.CreateDocumentAsync("DefaultLayoutModelSource");
            }
            var layoutKeyForDocumentType = GetFieldKeyByName(docType.Type);
            if (layoutModelSource.Field(layoutKeyForDocumentType) == null) {
                Debug.WriteLine("Using default layout model");

                // bcz: hack to have a default layout for known types: recipes, Umpires
                if (docType.Type == "recipes")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.Food2ForkRecipeModel(docType)), false);
                else if (docType.Type == "Umpires")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.UmpireModel(docType)), false);
                else if (docType.Type == "oneimage")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OneImageModel(docType)), false);
                else if (docType.Type == "twoimages")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(docType)), false);
                else if (docType.Type == "annotatedImage")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.annotatedImage(docType)), false);
                else if (docType.Type == "itunesLite")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunesLite(docType)), false);
                else if (docType.Type == "itunes")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunes(docType)), false);
                else if (docType.Type == "operator")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OperatorLayoutModel(docType)), false);
                else if (docType.Type == "example_api_object")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleApiObject(docType)), false);
                else if (docType.Type == "collection_example")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleCollectionModel(docType)), false);
                else if (docType.Type == "price_per_square_foot")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.PricePerSquareFootApiObject(docType)), false);
                else { // if it's an unknown document type, then create a LayoutModel that displays all of its fields.  
                       // this layout is created in showAllDocumentFields() 
                    Debug.WriteLine("now we gere");
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(new LayoutModel(true, docType)), false);
                }
            }

            return new ReferenceFieldModel(layoutModelSource.Id, layoutKeyForDocumentType);
        }
    }
}
