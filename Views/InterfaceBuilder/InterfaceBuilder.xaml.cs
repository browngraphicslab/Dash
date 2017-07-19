using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
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
        private LayoutCourtesyDocument _layoutCourtesyDocument;

        private EditableFieldFrame _selectedEditableFieldFrame { get; set; }


        public InterfaceBuilder(DocumentController docController, int width = 800, int height = 500)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;

            SetUpInterfaceBuilder(docController, new Context(docController));

            Binding listBinding = new Binding
            {
                Source = docController.GetAllPrototypes()
            };

            BreadcrumbListView.SetBinding(ItemsControl.ItemsSourceProperty, listBinding);
        }

        private void SetUpInterfaceBuilder(DocumentController docCont, Context context)
        {
            _layoutCourtesyDocument = new LayoutCourtesyDocument(docCont);

            _layoutCourtesyDocument.CreateAndSetFreeFormActiveLayout(new Point(), new Size(400, 400));
            _documentView = LayoutCourtesyDocument.MakeView(_layoutCourtesyDocument.Document, context) as DocumentView;


            // set the middle pane to hold the document view
            xDocumentHolder.Child = _documentView;

            xKeyValuePane.SetDataContextToDocumentController(_layoutCourtesyDocument.Document);

            _documentView.DragOver += DocumentViewOnDragOver;
            _documentView.Drop += DocumentViewOnDrop;
            _documentView.AllowDrop = true;
        }

        private void BreadcrumbListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DocumentController cont = e.ClickedItem as DocumentController;

            SetUpInterfaceBuilder(cont, new Context(cont));
        }

        private void ApplyEditable()
        {
            var editableElements = new List<FrameworkElement>();

            // iterate over all the documents which define views
            foreach (var layoutDocument in _layoutCourtesyDocument.GetLayoutDocuments())
            {
                var context = new Context(_layoutCourtesyDocument.Document);
                // use the layout document to generate a UI
                var fieldView = layoutDocument.MakeViewUI(context);

                var translationController = layoutDocument.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
                if (translationController != null)
                {
                    if (layoutDocument.GetId() != _layoutCourtesyDocument.Document.GetId()) // only bind translation when the layoutDocument isn't the entire layout
                    {
                        CourtesyDocument.BindTranslation(fieldView, translationController);
                    }
                }

                // generate an editable border
                var editableBorder = new EditableFieldFrame(layoutDocument.GetId(), layoutDocument.GetId() != _layoutCourtesyDocument.Document.GetId())
                {
                    EditableContent = fieldView,
                    HorizontalAlignment = HorizontalAlignment.Left, // align it to the left and top to avoid rescaling issues
                    VerticalAlignment = VerticalAlignment.Top
                };

                // bind the editable border width to the layout width
                var widthController =
                    layoutDocument.GetDereferencedField(DashConstants.KeyStore.WidthFieldKey, context) as NumberFieldModelController;
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
                    layoutDocument.GetDereferencedField(DashConstants.KeyStore.HeightFieldKey, context) as NumberFieldModelController;
                Debug.Assert(heightController != null);
                var heightBinding = new Binding
                {
                    Source = heightController,
                    Path = new PropertyPath(nameof(heightController.Data)),
                    Mode = BindingMode.TwoWay
                };
                editableBorder.SetBinding(HeightProperty, heightBinding);

                if (layoutDocument.GetId() != _layoutCourtesyDocument.Document.GetId())
                {
                    // when the editable border is loaded bind it's translation to the layout's translation
                    // TODO this probably causes a memory leak, but we have to capture the layoutDocument variable.
                    editableBorder.Loaded += delegate
                    {
                        translationController =
                                layoutDocument.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, context) as PointFieldModelController;
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


            var editedLayoutDocument = _layoutCourtesyDocument.GetLayoutDocuments().FirstOrDefault(doc => doc.GetId() == layoutDocumentId);
            Debug.Assert(editedLayoutDocument != null);

            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(editedLayoutDocument);
            if (newSettingsPane != null)
            {
                xSettingsPane.Children.Add(newSettingsPane);
            }
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

            var _documentController = _layoutCourtesyDocument.Document;
            var docController = _layoutCourtesyDocument.Document;
            var context = new Context(docController);

            var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            var fieldModelController = docController.GetDereferencedField(key, null);
            CourtesyDocuments.CourtesyDocument box = null;
            if (fieldModelController is TextFieldModelController)
            {
                var textFieldModelController = fieldModelController as TextFieldModelController;
                if (docController.GetPrototype() != null && docController.GetPrototype().GetDereferencedField(key, null) == null)
                {
                    docController.GetPrototype().SetField(key, textFieldModelController, false);
                }

                var layoutDoc = (docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, null) as DocumentFieldModelController)?.Data;

                if (layoutDoc == null || !docController.IsDelegateOf(layoutDoc.GetId()))
                    layoutDoc = docController;
                // bcz: hack -- the idea is that if we're dropping a field on a prototype layout, then the layout should reference the prototype of
                //       of the source document as well.  Otherwise, the other documents that use this prototype layout will get the data from this source document
                var layoutDocPrototype = layoutDoc.GetPrototype() == null ? layoutDoc : layoutDoc.GetPrototype();

                if (textFieldModelController.TextFieldModel.Data.EndsWith(".jpg"))
                    box = new CourtesyDocuments.ImageBox(new DocumentReferenceController(layoutDocPrototype.GetId(), key));
                else box = new CourtesyDocuments.TextingBox(new DocumentReferenceController(layoutDocPrototype.GetId(), key));
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                box = new CourtesyDocuments.ImageBox(new DocumentReferenceController(docController.GetId(), key));
            }
            else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                box = new CourtesyDocuments.CollectionBox(new DocumentReferenceController(docController.GetId(), key));
            }
            else if (fieldModelController is NumberFieldModelController)
            {
                box = new CourtesyDocuments.TextingBox(new DocumentReferenceController(docController.GetId(), key));
            }
            else if (fieldModelController is DocumentFieldModelController)
            {
                box = new CourtesyDocuments.LayoutCourtesyDocument(ContentController.GetController<DocumentFieldModelController>(fieldModelController.GetId()).Data);
            }

            var layoutDocFieldController = docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context);
            if (box != null)
            {
                //Sets the point position of the image/text box
                var pfmc = new PointFieldModelController(e.GetPosition(_documentView).X, e.GetPosition(_documentView).Y);
                box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, pfmc, false);

                var layoutDataField = _layoutCourtesyDocument.ActiveLayoutDocController?.GetDereferencedField(DashConstants.KeyStore.DataKey, null);

                if (layoutDataField is DocumentCollectionFieldModelController)
                {
                    (layoutDataField as DocumentCollectionFieldModelController).AddDocument(box.Document);
                }
                else
                {
                    var newLayoutCollection = new CollectionBox(new DocumentCollectionFieldModelController(new DocumentController[] { (docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController).Data, box.Document }));
                    var oldPt = ((layoutDocFieldController as DocumentFieldModelController).Data.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, null) as PointFieldModelController).Data;
                    (layoutDocFieldController as DocumentFieldModelController).Data.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Windows.Foundation.Point()), false);
                    layoutDocFieldController = new DocumentFieldModelController(newLayoutCollection.Document);
                    (layoutDocFieldController as DocumentFieldModelController).Data.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(oldPt), false);
                }
            }

            ApplyEditable();
            docController.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutDocFieldController, false);
        }



    }
}
