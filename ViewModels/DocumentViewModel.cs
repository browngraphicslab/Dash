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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Shapes;

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
        public DocumentController DocumentController;

        public delegate void OnLayoutChangedHandler(DocumentViewModel sender);

        public event OnLayoutChangedHandler OnLayoutChanged;

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

        private bool _isDetailedUserInterfaceVisible = true;

        public bool IsDetailedUserInterfaceVisible
        {
            get { return _isDetailedUserInterfaceVisible; }
            set { SetProperty(ref _isDetailedUserInterfaceVisible, value); }
        }

        private bool _isMoveable = true;

        public bool IsMoveable
        {
            get { return _isMoveable; }
            set { SetProperty(ref _isMoveable, value); }
        }

        // == CONSTRUCTORS == 
        public DocumentViewModel() { }

        public DocumentViewModel(DocumentController documentController)
        {
            DocumentController = documentController;
            BackgroundBrush = new SolidColorBrush(Colors.White);
            BorderBrush = new SolidColorBrush(Color.FromArgb(50,34,34,34));

            // set the X and Y position if the fields for those positions exist
            var xPositionFieldModelController = DocumentController.GetField(DashConstants.KeyStore.XPositionFieldKey);
            var yPositionFieldModelController = DocumentController.GetField(DashConstants.KeyStore.YPositionFieldKey);
            if (xPositionFieldModelController != null &&
                yPositionFieldModelController != null)
            {
                X = (xPositionFieldModelController as NumberFieldModelController).Data;
                Y = (yPositionFieldModelController as NumberFieldModelController).Data;
            }

            var documentFieldModelController = DocumentController.GetField(DashConstants.KeyStore.LayoutKey) as DocumentFieldModelController;
            if (documentFieldModelController != null)
                documentFieldModelController.Data.OnLayoutChanged += DocumentController_OnLayoutChanged;
        }

        private void DocumentController_OnLayoutChanged(DocumentController sender)
        {
            OnLayoutChanged?.Invoke(this);
        }

        private void DocumentModel_DocumentFieldUpdated(ReferenceFieldModel fieldReference)
        {
            throw new NotImplementedException();
            //if (fieldReference.FieldKey == DocumentModel.GetFieldKeyByName("X"))
            //    X = (DocumentModel.Field(DocumentModel.GetFieldKeyByName("X")) as NumberFieldModel).Data;
            //if (fieldReference.FieldKey == DocumentModel.GetFieldKeyByName("Y"))
            //    Y = (DocumentModel.Field(DocumentModel.GetFieldKeyByName("Y")) as NumberFieldModel).Data;
        }

        // == METHODS ==
        /// <summary>
        /// Generates a list of UIElements by making FieldViewModels of a document;s
        /// given fields.
        /// </summary>
        /// TODO: rename this to create ui elements
        /// <returns>List of all UIElements generated</returns>
        public virtual List<FrameworkElement> GetUiElements(Rect bounds)
        {
            return DocumentController.MakeViewUI();
        }

        private void SetUpFrameworkElement(FrameworkElement element, Key key)
        {
            element.DataContext = new ReferenceFieldModel(DocumentController.GetId(), key);

            //Binding manipulationBinding = new Binding
            //{
            //    Source = FreeformView.MainFreeformView.ViewModel, 
            //    Path = new PropertyPath("IsEditorMode"),
            //    Converter = new ManipulationConverter()
            //};
            //element.SetBinding(UIElement.ManipulationModeProperty, manipulationBinding);

            element.ManipulationMode = ManipulationModes.All;

            element.ManipulationStarted += (sender, args) => args.Complete();

            element.PointerPressed += Element_PointerPressed;
            element.PointerReleased += Element_PointerReleased;
        }

        private class ManipulationConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                bool isEditorMode = (bool)value;
                return isEditorMode ? ManipulationModes.All : ManipulationModes.System;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        /* 
        private Ellipse MakeEllipse(Key fieldKey, TemplateModel template, bool isOutput)
        {
            Ellipse el = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = isOutput ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Red),
                DataContext = new ReferenceFieldModel(DocumentModel.Id, fieldKey)
            };


            Binding visibilityBinding = new Binding
            {
                Source = this,
                Path = new PropertyPath("EditModeVisibility")
            };
            el.SetBinding(Ellipse.VisibilityProperty, visibilityBinding);
            Binding canvasTopBinding = new Binding
            {
                Source = template,
                Path = new PropertyPath("Top")
            };
            el.SetBinding(Canvas.TopProperty, canvasTopBinding);
            Binding canvasLeftBinding = new Binding
            {
                Source = template,
                Path = new PropertyPath("Left"),
                Converter = new CanvasLeftConverter(),
                ConverterParameter = isOutput ? 60.0 : -20.0
            };
            el.SetBinding(Canvas.LeftProperty, canvasLeftBinding);

            if (isOutput)
            {
                el.PointerPressed += Output_El_PointerExited;
                el.PointerReleased += Output_El_PointerReleased;
            }
            else
            {
                el.PointerPressed += Input_El_PointerExited;
                el.PointerReleased += Input_El_PointerReleased;
            }
            el.ManipulationMode = ManipulationModes.All;
            el.ManipulationStarted += (sender, args) => args.Complete();
            return el;
        }

        private class CanvasLeftConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                double left = (double)value;
                double offset = (double) parameter;
                return left + offset;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }
        */

        public event OperatorView.IODragEventHandler IODragStarted;
        public event OperatorView.IODragEventHandler IODragEnded;

        protected virtual void OnIoDragStarted(OperatorView.IOReference ioreference)
        {
            IODragStarted?.Invoke(ioreference);
        }

        protected virtual void OnIoDragEnded(OperatorView.IOReference ioreference)
        {
            IODragEnded?.Invoke(ioreference);
        }

        private void Element_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            IODragEnded?.Invoke(new OperatorView.IOReference((sender as FrameworkElement).DataContext as ReferenceFieldModel, false,
                    e, sender as FrameworkElement, (sender as FrameworkElement).GetFirstAncestorOfType<DocumentView>()));
        }

        private void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as FrameworkElement).Properties.IsLeftButtonPressed)
            {
                IODragStarted?.Invoke(new OperatorView.IOReference((sender as FrameworkElement).DataContext as ReferenceFieldModel,
                    true, e, sender as FrameworkElement, (sender as FrameworkElement).GetFirstAncestorOfType<DocumentView>()));
            }
        }

        public LayoutModel GetLayoutModel()
        {

            throw new NotImplementedException();
            //var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            //var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);

            //var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //var refField = docController.GetDocumentAsync(layoutModelRef.DocId).Field(layoutModelRef.FieldKey) as LayoutModelFieldModel;

            //return refField.Data;
        }

        public void SetLayoutModel(LayoutModel layoutModel)
        {
            throw new NotImplementedException();

            //var keyController = App.Instance.Container.GetRequiredService<KeyEndpoint>();
            //var layoutModelRef = GetLayoutModelReferenceForDoc(DocumentModel);
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
            throw new NotImplementedException();

            //var layoutField = doc.Field(DocumentModel.LayoutKey);

            //// If the Layout field is a LayoutModel, then use it.
            //if (layoutField is LayoutModelFieldModel)
            //{
            //    return new ReferenceFieldModel(doc.Id, DocumentModel.LayoutKey);
            //}

            //// otherwise lookup a LayoutModel for doc's type on a specified settings document or the default settings document
            //var settingsDocument = layoutField is DocumentModelFieldModel ? (layoutField as DocumentModelFieldModel).Data : DefaultLayoutModelSource;
            //return getLayoutModelReferenceForDocumentType(doc.DocumentType, settingsDocument);
        }

        static ReferenceFieldModel getLayoutModelReferenceForDocumentType(DocumentType docType, DocumentModel layoutModelSource)
        {
            throw new NotImplementedException();

            ////effectively, this sets defaultlayoutmodelsource if it hasnt been instantiated yet to a new doc each time
            //if (layoutModelSource == null)
            //{
            //    var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //    layoutModelSource = DefaultLayoutModelSource = docController.CreateDocumentAsync("DefaultLayoutModelSource");
            //}
            //var layoutKeyForDocumentType = GetFieldKeyByName(docType.Type);
            //if (layoutModelSource.Field(layoutKeyForDocumentType) == null)
            //{
            //    Debug.WriteLine("Using default layout model");

            //    // bcz: hack to have a default layout for known types: recipes, Umpires
            //    if (docType.Type == "recipes")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.Food2ForkRecipeModel(docType)), false);
            //    else if (docType.Type == "Umpires")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.UmpireModel(docType)), false);
            //    else if (docType.Type == "oneimage")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OneImageModel(docType)), false);
            //    else if (docType.Type == "twoimages")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.TwoImagesAndTextModel(docType)), false);
            //    else if (docType.Type == "annotatedImage")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.annotatedImage(docType)), false);
            //    else if (docType.Type == "itunesLite")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunesLite(docType)), false);
            //    else if (docType.Type == "itunes")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.itunes(docType)), false);
            //    else if (docType.Type == "operator")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.OperatorLayoutModel(docType)), false);
            //    else if (docType.Type == "example_api_object")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleApiObject(docType)), false);
            //    else if (docType.Type == "collection_example")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.ExampleCollectionModel(docType)), false);
            //    else if (docType.Type == "price_per_square_foot")
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(LayoutModel.PricePerSquareFootApiObject(docType)), false);
            //    else
            //    { // if it's an unknown document type, then create a LayoutModel that displays all of its fields.  
            //      // this layout is created in showAllDocumentFields() 
            //        Debug.WriteLine("now we gere");
            //        layoutModelSource.SetField(layoutKeyForDocumentType, new LayoutModelFieldModel(new LayoutModel(true, docType)), false);
            //    }
            //}

            //return new ReferenceFieldModel(layoutModelSource.Id, layoutKeyForDocumentType);
        }
    }
}
