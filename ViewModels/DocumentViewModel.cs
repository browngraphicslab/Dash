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

namespace Dash
{
    public class DocumentViewModel : ViewModelBase
    {
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

        public bool DoubleTapEnabled = true;

        public DocumentViewModel() { }
        public DocumentViewModel(DocumentModel docModel)
        {
            DocumentModel = docModel;
            if (docModel.DocumentType.Type == "collection")
            {
                DoubleTapEnabled = false;
                BackgroundBrush = new SolidColorBrush(Colors.Transparent);
                BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                BackgroundBrush = new SolidColorBrush(Colors.AliceBlue);
                BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
            }
        }
        public DocumentModel DocumentModel { get; set; }

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
                    var uiele = lEle.Value.MakeViewUI(DocumentModel.Field(lEle.Key));
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
            foreach (var f in DocumentModel.EnumFields())
                if (f.Key != GetFieldKeyByName("Delegates"))
                {
                    var fieldModel = f.Value;
                    while (fieldModel is ReferenceFieldModel)
                    {
                        fieldModel = docController.GetDocumentAsync((fieldModel as ReferenceFieldModel).DocId).Field((fieldModel as ReferenceFieldModel).FieldKey);
                    }
                    if (fieldModel is DocumentCollectionFieldModel)
                    {
                        uiElements.AddRange(new DocumentCollectionTemplateModel(bounds.Left, yloc, 500, 100, Visibility.Visible).MakeViewUI(fieldModel));
                        yloc += 100;
                    }
                    else if (fieldModel is ImageFieldModel || (fieldModel is TextFieldModel && (fieldModel as TextFieldModel).Data.EndsWith(".jpg")))
                    {
                        uiElements.AddRange(new ImageTemplateModel(bounds.Left, yloc, 500, 500).MakeViewUI(fieldModel));
                        yloc += 500;
                    }
                    else
                    {
                        uiElements.AddRange(new TextTemplateModel(bounds.Left, yloc, FontWeights.Bold, TextWrapping.Wrap, Visibility.Visible).MakeViewUI(fieldModel));
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

            // set value of layoutModelRef to layoutModel
        }

        static DocumentModel DefaultLayoutModelSource = null;
        private ManipulationModes _manipulationMode;
        private double _height;
        private double _width;
        private Brush _backgroundBrush;
        private Brush _borderBrush;

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
            return getLayoutModelReferenceForDocumentType(doc.DocumentType, doc.EnumFields(), settingsDocument);
        }

        static ReferenceFieldModel getLayoutModelReferenceForDocumentType(DocumentType docType, IEnumerable<KeyValuePair<Key,FieldModel>> docFields, DocumentModel layoutModelSource)
        {
            if (layoutModelSource == null)
            {
                var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
                layoutModelSource = DefaultLayoutModelSource = docController.CreateDocumentAsync("DefaultLayoutModelSource");
            }
            var layoutKeyForDocumentType = GetFieldKeyByName(docType.Type);
            if (layoutModelSource.Field(layoutKeyForDocumentType) == null)
            {
                // bcz: hack to have a default layout for known types: recipes, Umpires
                if (docType.Type == "recipes")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.Food2ForkRecipeModel(docType)));
                else if (docType.Type == "Umpires")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.UmpireModel(docType)));
                else if (docType.Type == "oneimage")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OneImageModel(docType)));
                else if (docType.Type == "twoimages")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(docType)));
                else if (docType.Type == "annotatedImage")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.annotatedImage(docType)));
                else if (docType.Type == "itunesLite")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunesLite(docType)));
                else if (docType.Type == "itunes")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunes(docType)));
                else if (docType.Type == "operator")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OperatorLayoutModel(docType)));
                else if (docType.Type == "example_api_object")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleApiObject(docType)));
                else if (docType.Type == "collection_example")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleCollectionModel(docType)));
                else if (docType.Type == "price_per_square_foot")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.PricePerSquareFootApiObject(docType)));
                else { // if it's an unknown document type, then create a LayoutModel that displays all of its fields.  
                       // this layout is created in showAllDocumentFields() 
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(new LayoutModel(true, docType)));
                }
            }
            return new ReferenceFieldModel(layoutModelSource.Id, layoutKeyForDocumentType);
        }
    }
}
