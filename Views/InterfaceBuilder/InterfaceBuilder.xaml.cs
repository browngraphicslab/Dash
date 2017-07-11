using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using static Dash.CourtesyDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        private List<GuideLineViewModel> _guides = new List<GuideLineViewModel>();
        
        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;
        

        /// <summary>
        /// Courtesy document that manages getting the necessary layout fields to edit the document's layout
        /// </summary>
        private LayoutCourtesyDocument LayoutCourtesyDocument;

        private EditableFieldFrame _selectedEditableFieldFrame { get; set; }

        private DocumentController _documentController;

        public InterfaceBuilder(DocumentViewModel viewModel, int width = 800, int height = 500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;
            
            LayoutCourtesyDocument = new LayoutCourtesyDocument(viewModel.DocumentController, viewModel.DocContextList);
           
            _documentView = LayoutCourtesyDocument.MakeView(LayoutCourtesyDocument.Document, viewModel.DocContextList) as DocumentView;


            _documentController = viewModel.DocumentController;

            xDocumentHolder.Child = _documentView;

            xKeyValuePane.SetDataContextToDocumentController(_documentController);

            _documentView.DragOver += DocumentViewOnDragOver;
            _documentView.Drop += DocumentViewOnDrop;
            _documentView.AllowDrop = true;


            ApplyEditable();
        }

        private void ApplyEditable()
        {
            var editableElements = new List<FrameworkElement>();
            var contextList = (_documentView.DataContext as DocumentViewModel)?.DocContextList;
            // iterate over all the documents which define views
            foreach (var layoutDocument in LayoutCourtesyDocument.GetLayoutDocuments(contextList))
            {
                var docContextList = contextList != null ? new List<DocumentController>(contextList) : new List<DocumentController>();
                docContextList.Add(LayoutCourtesyDocument.Document);
                // use the layout document to generate a UI
                var fieldView = layoutDocument.makeViewUI(docContextList);

                var translationController = layoutDocument.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, docContextList) as PointFieldModelController;
                if (translationController != null)
                {
                    if (layoutDocument.GetId() != LayoutCourtesyDocument.Document.GetId()) // only bind translation when the layoutDocument isn't the entire layout
                    {
                        CourtesyDocument.BindTranslation(fieldView, translationController);
                    }
                }

                // generate an editable border
                var editableBorder = new EditableFieldFrame(layoutDocument.GetId(), layoutDocument.GetId() != LayoutCourtesyDocument.Document.GetId())
                {
                    EditableContent = fieldView,
                    HorizontalAlignment = HorizontalAlignment.Left, // align it to the left and top to avoid rescaling issues
                    VerticalAlignment = VerticalAlignment.Top
                };

                // bind the editable border width to the layout width
                var widthController =
                    layoutDocument.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, docContextList) as NumberFieldModelController;
                Debug.Assert(widthController != null);
                var widthBinding = new Binding
                {
                    Source = widthController,
                    Path = new PropertyPath(nameof(widthController.Data)),
                    Mode = BindingMode.TwoWay
                };
                editableBorder.SetBinding(WidthProperty, widthBinding);

                // bind the editable border height to the layout height
                var heightController =
                    layoutDocument.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, docContextList) as NumberFieldModelController;
                Debug.Assert(heightController != null);
                var heightBinding = new Binding
                {
                    Source = heightController,
                    Path = new PropertyPath(nameof(heightController.Data)),
                    Mode = BindingMode.TwoWay
                };
                editableBorder.SetBinding(HeightProperty, heightBinding);

                if (layoutDocument.GetId() != LayoutCourtesyDocument.Document.GetId())
                {
                    // when the editable border is loaded bind it's translation to the layout's translation
                    // TODO this probably causes a memory leak, but we have to capture the layoutDocument variable.
                    editableBorder.Loaded += delegate
                    {
                        translationController =
                                layoutDocument.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, docContextList) as PointFieldModelController;
                        Debug.Assert(translationController != null);
                        var translateBinding = new Binding
                        {
                            Source = translationController,
                            Path = new PropertyPath(nameof(translationController.Data)),
                            Mode = BindingMode.TwoWay,
                            Converter = new PointToTranslateTransformConverter()
                        };
                        editableBorder.Container.SetBinding(UIElement.RenderTransformProperty, translateBinding);
                    };
                } 
                editableBorder.Tapped += EditableBorder_Tapped;
                editableElements.Add(editableBorder);
            }

            var canvas = new Canvas();
            foreach (var frameworkElement in editableElements)
            {
                canvas.Children.Add(frameworkElement);
            }

            _documentView.ViewModel.Content = canvas;
        }

        private void EditableBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xSettingsPane.Children.Clear();

            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            UpdateEditableFieldFrameSelection(editableFieldFrame);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = LayoutCourtesyDocument.GetLayoutDocuments((_documentView.DataContext as DocumentViewModel).DocContextList).FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);

            xSettingsPane.Children.Add(SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(editedLayoutDocument));
        }

        private void UpdateEditableFieldFrameSelection(EditableFieldFrame newlySelectedEditableFieldFrame)
        {
            if (_selectedEditableFieldFrame != null)
            {
                _selectedEditableFieldFrame.IsSelected = false;
            }
            newlySelectedEditableFieldFrame.IsSelected = true;
            _selectedEditableFieldFrame = newlySelectedEditableFieldFrame;
        }

        private void DocumentViewOnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {
            var docContextList = (_documentView.DataContext as DocumentViewModel).DocContextList;
            var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            var fieldModelController = _documentController.GetDereferencedField(key, docContextList);
            CourtesyDocuments.CourtesyDocument box = null;
            if (fieldModelController is TextFieldModelController)
            {
                var textFieldModelController = _documentController.GetDereferencedField(key, docContextList) as TextFieldModelController;
               if (_documentController.GetPrototype() != null && _documentController.GetPrototype().GetDereferencedField(key, docContextList) == null)
                {
                    _documentController.GetPrototype().SetField(key, _documentController.GetDereferencedField(key, docContextList), false);
                }
                var layoutDoc = (_documentController.GetDereferencedField(DashConstants.KeyStore.LayoutKey, docContextList) as DocumentFieldModelController)?.Data;
                if (layoutDoc == null || !_documentController.IsDelegateOf(layoutDoc.GetId()))
                    layoutDoc = _documentController;
                if (textFieldModelController.TextFieldModel.Data.EndsWith(".jpg"))
                      box = new CourtesyDocuments.ImageBox(new ReferenceFieldModelController(layoutDoc.GetId(), key));
                else  box = new CourtesyDocuments.TextingBox(new ReferenceFieldModelController(layoutDoc.GetId(), key));
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                box = new CourtesyDocuments.ImageBox(new ReferenceFieldModelController(_documentController.GetId(), key));
            }
            else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                box = new CourtesyDocuments.CollectionBox(new ReferenceFieldModelController(_documentController.GetId(), key));
            }
            else if (fieldModelController is NumberFieldModelController)
            {
                box = new CourtesyDocuments.TextingBox(new ReferenceFieldModelController(_documentController.GetId(), key));
            } else if (fieldModelController is DocumentFieldModelController)
            {
                box = new CourtesyDocuments.LayoutCourtesyDocument(ContentController.GetController<DocumentController>(fieldModelController.GetId()), docContextList);
            }

            if (box != null)
            {
                //Sets the point position of the image/text box
                var pfmc = new PointFieldModelController(e.GetPosition(_documentView).X,
                        e.GetPosition(_documentView).Y);
                box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, pfmc, false);
                var layoutDataField = LayoutCourtesyDocument.LayoutDocumentController?.GetDereferencedField(DashConstants.KeyStore.DataKey, docContextList);

                ContentController.GetController<DocumentCollectionFieldModelController>(layoutDataField.GetId()).AddDocument(box.Document);
            }

            ApplyEditable();
        }
    }

    public static class SettingsPaneFromDocumentControllerFactory
    {
        public static UIElement CreateSettingsPane(DocumentController editedLayoutDocument)
        {
            if (editedLayoutDocument.DocumentType == ImageBox.DocumentType)
            {
                return CreateImageSettingsLayout(editedLayoutDocument);
            }
            if (editedLayoutDocument.DocumentType == TextingBox.DocumentType)
            {
                return CreateTextSettingsLayout(editedLayoutDocument);
            }

            Debug.Assert(false,
                $"We do not create a settings pane for the document with type {editedLayoutDocument.DocumentType}");
            return null;
        }

        private static UIElement CreateImageSettingsLayout(DocumentController editedLayoutDocument)
        {
            var docContextList = new List<DocumentController>(); // bcz: ??? Is this right?
            return new ImageSettings(editedLayoutDocument, docContextList);
        }

        private static UIElement CreateTextSettingsLayout(DocumentController editedLayoutDocument)
        {
            var docContextList = new List<DocumentController>(); // bcz: ??? Is this right?
            return new TextSettings(editedLayoutDocument, docContextList);
        }

    }
}
