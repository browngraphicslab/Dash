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
        }

        // == METHODS ==
        /// <summary>
        /// Generates a list of UIElements by making FieldViewModels of a document;s
        /// given fields.
        /// </summary>
        /// TODO: rename this to create ui elements
        /// <returns>List of all UIElements generated</returns>
        public virtual List<UIElement> GetUiElements()
        {
            var uiElements = new List<UIElement>();
            var layout = GetLayoutModel();

            if (layout.ShowAllFields) 
            {
                showAllDocumentFields(uiElements);
            }
            else
            {
                foreach (var lEle in layout.Fields)
                    if (lEle.Value is TextTemplateModel || lEle.Value is DocumentCollectionTemplateModel || lEle.Value is ImageTemplateModel) {
                        var uiele = lEle.Value.MakeView(DocumentModel.Field(lEle.Key));
                        if (uiele != null)
                            uiElements.Add(uiele);
                    }
                    else if (DocumentModel.Field(lEle.Key) != null)
                    {
                        uiElements.Add(lEle.Value.MakeView(DocumentModel.Field(lEle.Key)));
                    }
            }
            return uiElements;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uiElements"></param>
        void showAllDocumentFields(List<UIElement> uiElements)
        {
            double yloc = 0;
            foreach (var f in DocumentModel.EnumFields())
                if (f.Key != GetFieldKeyByName("Delegates"))
                {
                    if (f.Value is DocumentCollectionFieldModel)
                    {
                        uiElements.Add(new DocumentCollectionTemplateModel(0, yloc, 500, 100, Visibility.Visible).MakeView(f.Value));
                        yloc += 500;
                    }
                    else
                    {
                        uiElements.Add(new TextTemplateModel(0, yloc, FontWeights.Bold, TextWrapping.Wrap, Visibility.Visible).MakeView(f.Value));
                        yloc += 20;
                    }
                }
        }
        
        /// <summary>
        /// Gets the layoutmodel associated with a given DocumentViewModel
        /// </summary>
        /// <returns></returns>
        public LayoutModel GetLayoutModel()
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);

            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            var refField = docController.GetDocumentAsync(layoutModelRef.DocId).Field(layoutModelRef.FieldKey) as LayoutModelFieldModel;

            return refField.Data;
        }

        /// <summary>
        /// Sets the layoutModel of the DocumentView to a given LayoutModel
        /// </summary>
        /// <param name="layoutModel"></param>
        public void SetLayoutModel(LayoutModel layoutModel)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);
        }

        /// <summary>
        /// Give a string representation of a key, returns the Dash.Key representation
        /// of that string as found via GetKeyAsync()
        /// </summary>
        /// <param name="name">string value of the key</param>
        /// <returns></returns>
        static Key GetFieldKeyByName(string name)
        {
            var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            var key = keyController.GetKeyAsync(name);
            if (key == null)
                key = keyController.CreateKeyAsync(name);
            return key;
        }

        /// <summary>
        /// Find the layoutModel to use to display this document and return it as referenceField to where the layoutModel is stored.
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

        /// <summary>
        /// Given a documentType, finds the corresponding LayoutModel and sets that model to the
        /// value of a given type's key in layoutModelSource's fields.
        /// </summary>
        /// <param name="docType"></param>
        /// <param name="layoutModelSource"></param>
        /// <returns></returns>
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
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.Food2ForkRecipeModel(docType)));
                else if (docType.Type == "Umpires")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.UmpireModel(docType)));
                else if (docType.Type == "oneimage")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OneImageModel(docType)));
                else if (docType.Type == "twoimages")
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(docType)));
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
                    Debug.WriteLine("now we gere");
                    layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(new LayoutModel(true, docType)));
                }
            }

            return new ReferenceFieldModel(layoutModelSource.Id, layoutKeyForDocumentType);
        }
    }
}
