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
            
            LayoutCourtesyDocument = new LayoutCourtesyDocument(viewModel.DocumentController);
           
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

                var translationController = layoutDocument.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
                if (translationController != null)
                {
                    CourtesyDocument.BindTranslation(fieldView, translationController);
                }               

                // generate an editable border
                var editableBorder = new EditableFieldFrame(layoutDocument.GetId())
                {
                    EditableContent = fieldView,
                    HorizontalAlignment = HorizontalAlignment.Left, // align it to the left and top to avoid rescaling issues
                    VerticalAlignment = VerticalAlignment.Top
                };

                // bind the editable border width to the layout width
                var widthController =
                    layoutDocument.GetField(DashConstants.KeyStore.WidthFieldKey) as NumberFieldModelController;
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
                    layoutDocument.GetField(DashConstants.KeyStore.HeightFieldKey) as NumberFieldModelController;
                Debug.Assert(heightController != null);
                var heightBinding = new Binding
                {
                    Source = heightController,
                    Path = new PropertyPath(nameof(heightController.Data)),
                    Mode = BindingMode.TwoWay
                };
                editableBorder.SetBinding(HeightProperty, heightBinding);

                // when the editable border is loaded bind it's translation to the layout's translation
                // TODO this probably causes a memory leak, but we have to capture the layoutDocument variable.
                editableBorder.Loaded += delegate
                {
                    translationController =
                            layoutDocument.GetField(DashConstants.KeyStore.PositionFieldKey) as PointFieldModelController;
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

                editableBorder.Tapped += EditableBorder_Tapped;
                editableElements.Add(editableBorder);
            }

            _documentView.SetUIElements(editableElements);
        }

        private void EditableBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xSettingsPane.Children.Clear();

            var editableFieldFrame = sender as EditableFieldFrame;
            Debug.Assert(editableFieldFrame != null);

            UpdateEditableFieldFrameSelection(editableFieldFrame);

            var layoutDocumentId = editableFieldFrame.DocumentId;

            var editedLayoutDocument = LayoutCourtesyDocument.GetLayoutDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
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
            var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            var fieldModel = _documentController.GetField(key).FieldModel;
            CourtesyDocuments.CourtesyDocument box = null;
            if (fieldModel is TextFieldModel)
            {
                var textFieldModel= ContentController.DereferenceToRootFieldModel<TextFieldModel>(new ReferenceFieldModel(_documentController.GetId(), key));
                var textFieldModelController = ContentController.GetController<TextFieldModelController>(textFieldModel.Id);
                if (_documentController.GetPrototype() != null && _documentController.GetPrototype().GetField(key) == null)
                {
                    _documentController.GetPrototype().SetField(key, _documentController.GetField(key), false);
                }
                var layoutDoc = ContentController.GetController<DocumentController>((_documentController.GetField(DashConstants.KeyStore.LayoutKey)?.FieldModel as DocumentModelFieldModel)?.Data.Id);
                if (layoutDoc == null || !_documentController.IsDelegateOf(layoutDoc.GetId()))
                    layoutDoc = _documentController;
                if (textFieldModelController.TextFieldModel.Data.EndsWith(".jpg"))
                      box = new CourtesyDocuments.ImageBox(new ReferenceFieldModelController(layoutDoc.GetId(), key));
                else  box = new CourtesyDocuments.TextingBox(new ReferenceFieldModelController(layoutDoc.GetId(), key));
            }
            else if (fieldModel is ImageFieldModel)
            {
                box = new CourtesyDocuments.ImageBox(new ReferenceFieldModelController(_documentController.GetId(), key));
            }
            else if (fieldModel is NumberFieldModel)
            {
                box = new CourtesyDocuments.TextingBox(new ReferenceFieldModelController(_documentController.GetId(), key));
            } else if (fieldModel is DocumentModelFieldModel)
            {
                box = new CourtesyDocuments.LayoutCourtesyDocument(ContentController.GetController<DocumentController>((fieldModel as DocumentModelFieldModel).Data.Id));
            }

            if (box != null)
            {
                //Sets the point position of the image/text box
                var pfmc = new PointFieldModelController(e.GetPosition(_documentView).X,
                        e.GetPosition(_documentView).Y);
                box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, pfmc, false);
                ContentController.AddController(pfmc);
                var layoutDataField = ContentController.DereferenceToRootFieldModel(LayoutCourtesyDocument.LayoutDocumentController?.GetField(DashConstants.KeyStore.DataKey));

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
            return new ImageSettings(editedLayoutDocument);
        }

        private static UIElement CreateTextSettingsLayout(DocumentController editedLayoutDocument)
        {
            return new TextSettings(editedLayoutDocument);
        }

    }
}
