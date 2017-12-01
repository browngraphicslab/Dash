using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;
using Dash.Controllers;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {
        public static string LayoutDragKey = "B3B49D46-6D56-4CC9-889D-4923805F2DA9";
        private SelectableContainer _selectedContainer;

        public enum DisplayTypeEnum { List, Grid, Freeform }

        private DocumentController _editingDocument;
        private DocumentView _editingDocView;

        private DocumentView _view; 

        public InterfaceBuilder(DocumentController docController, int width = 1000, int height = 545, DocumentView view = null)
        {
            this.InitializeComponent();
            Util.InitializeDropShadow(xShadowHost, xShadowTarget);
            Width = width;
            Height = height;
            _view = view; 

            SetUpInterfaceBuilder(docController, new Context(docController));

            // TODO do we want to update breadcrumb bindings or just set them once
            Binding listBinding = new Binding
            {
                Source = docController.GetAllPrototypes()
            };
            BreadcrumbListView.SetBinding(ItemsControl.ItemsSourceProperty, listBinding);
        }

        /// <summary>
        /// Bind the textbox that shows the layout's name to the current layout being used
        /// ** deprecated?? 
        /// </summary>
        private void BindLayoutText(DocumentController currentLayout)
        {
            var textBinding = new Binding
            {
                Source = currentLayout,
                Path = new PropertyPath(nameof(currentLayout.LayoutName)),
                Mode = BindingMode.TwoWay
            };
            //xLayoutTextBox.SetBinding(TextBox.TextProperty, textBinding);
        }

        private void SetUpInterfaceBuilder(DocumentController docController, Context context)
        {
            _editingDocument = docController;
            xDocumentPane.OnDocumentViewLoaded -= DocumentPaneOnDocumentViewLoaded;
            xDocumentPane.OnDocumentViewLoaded += DocumentPaneOnDocumentViewLoaded;
            var freeFormView = new SimpleCollectionViewModel(true);
            xDocumentPane.DataContext = freeFormView;
            freeFormView.AddDocuments(new List<DocumentController> { docController }, null);
            xKeyValuePane.SetDataContextToDocumentController(docController);

            xKeyValuePane.OnKeyValuePairAdded -= XKeyValuePane_OnKeyValuePairAdded;
            xKeyValuePane.OnKeyValuePairAdded += XKeyValuePane_OnKeyValuePairAdded;
        }

        private void XKeyValuePane_OnKeyValuePairAdded(KeyValuePane sender, DocumentController dc)
        {
            _view?.UpdateTreeNode(dc); 
        }

        private void DocumentPaneOnDocumentViewLoaded(CollectionFreeformView collectionFreeformView, DocumentView documentView)
        {
            SetUpDocumentView(documentView);
        }

        private void SetUpDocumentView(DocumentView documentView)
        {
            var editingDocumentId = _editingDocument.GetId();
            if (documentView.ViewModel == null /*|| documentView.ViewModel.DocumentController.GetId() != editingDocumentId*/)
            {
                return;
            }
            _editingDocView = documentView;

            if (_editingDocView != null)
            {
                UpdateRootLayout();
                _editingDocView.DragOver += DocumentViewOnDragOver;
                _editingDocView.DragEnter += DocumentViewOnDragOver;
                _editingDocView.AllowDrop = true;
                _editingDocView.Drop += DocumentViewOnDrop;
                _editingDocView.ViewModel.LayoutChanged -= OnActiveLayoutChanged;
                _editingDocView.ViewModel.LayoutChanged += OnActiveLayoutChanged;
            }
        }

        private void OnActiveLayoutChanged(DocumentViewModel sender, Context context)
        {
            UpdateRootLayout();
        }

        private void UpdateRootLayout()
        {
            var rootSelectableContainer = _editingDocView?.ViewModel.Content as SelectableContainer;
            Debug.Assert(rootSelectableContainer != null);
            rootSelectableContainer.OnSelectionChanged += RootSelectableContainerOnOnSelectionChanged;
        }

        private void DocumentViewOnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private SelectableContainer GetFirstCompositeLayoutContainer(Point dropPoint)
        {
            var elem = VisualTreeHelper.FindElementsInHostCoordinates(dropPoint, _editingDocView)
                .FirstOrDefault(AssertIsCompositeLayout);
            return elem as SelectableContainer;
        }

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {
            var layoutContainer = GetFirstCompositeLayoutContainer(e.GetPosition(MainPage.Instance));
            if (layoutContainer == null) return; // we can only drop on composites

            var isDraggedFromKeyValuePane = e.Data.Properties[KeyValuePane.DragPropertyKey] != null;
            var isDraggedFromLayoutBar = e.Data.Properties[LayoutDragKey]?.GetType() == typeof(DisplayTypeEnum);

            if (isDraggedFromKeyValuePane)
            {
                // get data variables from the DragArgs
                var kvp = (KeyValuePair<KeyController, DocumentController>)e.Data.Properties[KeyValuePane.DragPropertyKey];
                var dataDocController = kvp.Value;
                var dataKey = kvp.Key;
                var context = new Context(dataDocController);
                var dataField = dataDocController.GetDereferencedField(dataKey, context);

                // get a layout document for the data - use the most abstract prototype as the field reference document
                //  (otherwise, the layout would point directly to the data instance which would make it impossible to
                //   create Data copies since the layout would point directly to the (source) data instance and not the common prototype).
                var dataPrototypeDoc = kvp.Value;
                while (dataPrototypeDoc.GetPrototype() != null)
                    dataPrototypeDoc = dataPrototypeDoc.GetPrototype();
                var layoutDocument = GetLayoutDocumentForData(dataField, dataPrototypeDoc, dataKey, context);
                if (layoutDocument == null)
                    return;

                // apply position if we are dropping on a freeform
                if (layoutContainer.LayoutDocument.DocumentType.Equals(DashConstants.TypeStore.FreeFormDocumentLayout))
                {
                    var posInLayoutContainer = e.GetPosition(layoutContainer);
                    var widthOffset = (layoutDocument.GetField(KeyStore.WidthFieldKey) as NumberFieldModelController).Data / 2;
                    var heightOffset = (layoutDocument.GetField(KeyStore.HeightFieldKey) as NumberFieldModelController).Data / 2;
                    var positionController = new PointFieldModelController(posInLayoutContainer.X - widthOffset,posInLayoutContainer.Y- heightOffset);
                    layoutDocument.SetField(KeyStore.PositionFieldKey, positionController, forceMask: true);
                }

                // add the document to the composite
                //if (layoutContainer.DataDocument != null) context.AddDocumentContext(layoutContainer.DataDocument);
                var data = layoutContainer.LayoutDocument.GetDereferencedField(KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
                data?.AddDocument(layoutDocument);
            }
            else if (isDraggedFromLayoutBar)
            {
                var displayType = (DisplayTypeEnum)e.Data.Properties[LayoutDragKey];
                DocumentController newLayoutDocument = null;
                var size = new Size(200, 200);
                var position = e.GetPosition(layoutContainer);
                //center
                position = new Point(position.X - size.Width / 2, position.Y - size.Height / 2);
                switch (displayType)
                {
                    case DisplayTypeEnum.Freeform:
                        newLayoutDocument = new FreeFormDocument(new List<DocumentController>(), position, size).Document;
                        break;
                    case DisplayTypeEnum.Grid:
                        newLayoutDocument = new GridViewLayout(new List<DocumentController>(), position, size).Document;
                        break;
                    case DisplayTypeEnum.List:
                        newLayoutDocument = new ListViewLayout(new List<DocumentController>(), position, size).Document;
                        break;
                    default:
                        break;
                }
                if (newLayoutDocument != null)
                {
                    var context = new Context(newLayoutDocument);
                    var col = layoutContainer.LayoutDocument.GetDereferencedField(KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
                    col?.AddDocument(newLayoutDocument);
                }
            }
        }

        public static DocumentController GetLayoutDocumentForData(FieldControllerBase fieldModelController,
            DocumentController docController, KeyController key, Context context)
        {
            DocumentController layoutDocument = null;
            if (fieldModelController is TextFieldModelController)
            {
                layoutDocument = new TextingBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is NumberFieldModelController)
            {
                layoutDocument = new TextingBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                layoutDocument = new ImageBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                layoutDocument = new CollectionBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is DocumentFieldModelController)
            {
                //layoutDocument = new TextingBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
                layoutDocument = new DocumentBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is RichTextFieldModelController)
            {
                layoutDocument = new RichTextBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is InkFieldModelController)
            {
                layoutDocument = new InkBox(new DocumentReferenceFieldController(docController.GetId(), key)).Document;
            }
            return layoutDocument;
        }


        private bool AssertIsCompositeLayout(object obj)
        {
            if (!(obj is SelectableContainer))
            {
                return false;
            }
            var cont = (SelectableContainer)obj;
            return IsCompositeLayout(cont.LayoutDocument);
        }

        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument, DocumentController dataDocument)
        {
            xSettingsPane.Children.Clear();
            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(layoutDocument, dataDocument);
            _selectedContainer = sender;
            
            // change visual opacity of delete button so it looks like it is activated or deactivated
            if (_selectedContainer.ParentContainer != null)
            {
                xDeleteButton.Opacity = 1;
            }
            else
            {
                xDeleteButton.Opacity = .5;
            }

            if (newSettingsPane == null) return;
            // if newSettingsPane is a general document setting, bind the layoutname textbox 
            if (newSettingsPane is FreeformSettings)
            {
                var currLayout = (newSettingsPane as FreeformSettings).SelectedDocument;
                BindLayoutText(currLayout);
            }
            xSettingsPane.Children.Add(newSettingsPane);
        }

        public bool IsCompositeLayout(DocumentController layoutDocument)
        {
            return layoutDocument.DocumentType.Equals(DashConstants.TypeStore.FreeFormDocumentLayout) ||
                   layoutDocument.DocumentType.Equals(GridViewLayout.DocumentType) ||
                   layoutDocument.DocumentType.Equals(ListViewLayout.DocumentType);
        }

        private void BreadcrumbListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DocumentController cont = e.ClickedItem as DocumentController;

            SetUpInterfaceBuilder(cont, new Context(cont));
        }
        private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var item = e.Items.FirstOrDefault();
            if (item is StackPanel)
            {
                //var defaultNewSize = new Size(400, 400);
                var button = item as StackPanel;

                switch (button.Name)
                {
                    case "ListButton":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.List;
                        break;
                    case "GridButton":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.Grid;
                        break;
                    case "FreeformButton":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.Freeform;
                        break;
                    default:
                        break;
                }
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }

        private void XDeleteButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedContainer.ParentContainer == null) return; 
            var data = _selectedContainer.ParentContainer.LayoutDocument.GetDereferencedField(KeyStore.DataKey, null) as DocumentCollectionFieldModelController;
            data?.RemoveDocument(_selectedContainer.LayoutDocument);
            _selectedContainer.ParentContainer.SetSelectedContainer(null);
            xDeleteButton.Opacity = .5;
        }
        
        private void ChromeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            bool visible; 
            Visibility visibility = (visible = ButtonsView.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
            ButtonsView.Visibility = visibility;
            xDeleteButton.Visibility = visibility;
            BreadcrumbListView.Visibility = visibility;

            if (visible) Height += 78;
            else Height -= 78;
        }
    }
}
