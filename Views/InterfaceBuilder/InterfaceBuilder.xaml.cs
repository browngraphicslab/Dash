using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using DashShared;
using Windows.UI.Xaml.Media;
using Windows.UI;

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


        public InterfaceBuilder(DocumentController docController, int width = 1000, int height = 545)
        {
            this.InitializeComponent();
            Width = width;
            Height = height;

            SetUpInterfaceBuilder(docController, new Context(docController));

            //SetUpButtons();

            // TODO do we want to update breadcrumb bindings or just set them once
            Binding listBinding = new Binding
            {
                Source = docController.GetAllPrototypes()
            };
            BreadcrumbListView.SetBinding(ItemsControl.ItemsSourceProperty, listBinding);
        }

        private void SetUpInterfaceBuilder(DocumentController docController, Context context)
        {
            _editingDocument = docController;
            docController.AddFieldUpdatedListener(DashConstants.KeyStore.ActiveLayoutKey, OnActiveLayoutChanged);
            var documentCanvasViewModel = new DocumentCanvasViewModel(true);
            xDocumentPane.DataContext = documentCanvasViewModel;
            documentCanvasViewModel.AddDocument(docController, true);
            xDocumentPane.Loaded += xDocumentPaneLoaded;
            xKeyValuePane.SetDataContextToDocumentController(docController);
        }

        private void xDocumentPaneLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SetUpDocumentView();
        }

        private void SetUpDocumentView()
        {
            _editingDocView = xDocumentPane.GetDocumentView(_editingDocument.GetId());

            if (_editingDocView != null)
            {
                UpdateRootLayout();
                _editingDocView.DragOver += DocumentViewOnDragOver;
                _editingDocView.AllowDrop = true;
                _editingDocView.Drop += DocumentViewOnDrop;
            }
        }

        private void SetUpButtons()
        {
            var listSymbol = new SymbolIcon()
            {
                Symbol = Symbol.List,
                Foreground = new SolidColorBrush(Colors.White)
            };
            ListButton.Content = new Border { Child = listSymbol };

            var freeformSymbol = new SymbolIcon()
            {
                Symbol = Symbol.View,
                Foreground = new SolidColorBrush(Colors.White)
            };
            FreeformButton.Content = new Border { Child = freeformSymbol };

            var gridSymbol = new SymbolIcon()
            {
                Symbol = Symbol.ViewAll,
                Foreground = new SolidColorBrush(Colors.White)
            };
            GridButton.Content = new Border { Child = gridSymbol };
        }

        private void OnActiveLayoutChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
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

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {   
            var layoutContainer = GetFirstCompositeLayoutContainer(e.GetPosition(MainPage.Instance));
            if (layoutContainer == null) return; // we can only drop on composites

            var isDraggedFromKeyValuePane = e.Data.Properties[KeyValuePane.DragPropertyKey] != null;
            var isDraggedFromLayoutBar = e.Data.Properties[LayoutDragKey]?.GetType() == typeof(DisplayTypeEnum);

            if (isDraggedFromKeyValuePane)
            {
                // get data variables from the DragArgs
                var kvp = (KeyValuePair<Key, DocumentController>)e.Data.Properties[KeyValuePane.DragPropertyKey];
                var dataDocController = kvp.Value;
                var dataKey = kvp.Key;
                var context = new Context(dataDocController);
                var dataField = dataDocController.GetDereferencedField(dataKey, context);

                // get a layout document for the data
                var layoutDocument = GetLayoutDocumentForData(dataField, dataDocController, dataKey, context);
                if (layoutDocument == null)
                    return;

                // apply position if we are dropping on a freeform
                if (layoutContainer.LayoutDocument.DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
                {
                    var positionController = new PointFieldModelController(e.GetPosition(layoutContainer).X, e.GetPosition(layoutContainer).Y);
                    layoutDocument.SetField(DashConstants.KeyStore.PositionFieldKey, positionController, forceMask: true);
                }

                // add the document to the composite
                //if (layoutContainer.DataDocument != null) context.AddDocumentContext(layoutContainer.DataDocument);
                var data = layoutContainer.LayoutDocument.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as DocumentCollectionFieldModelController;
                data?.AddDocument(layoutDocument);
            }
            else if (isDraggedFromLayoutBar)
            {
                var displayType = (DisplayTypeEnum)e.Data.Properties[LayoutDragKey];
                DocumentController newLayoutDocument = null;
                var size = new Size(200, 200);
                var position = e.GetPosition(layoutContainer);
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
                    var col = layoutContainer.LayoutDocument.GetField(DashConstants.KeyStore.DataKey) as
                        DocumentCollectionFieldModelController;
                    col?.AddDocument(newLayoutDocument);
                }
            }
        }

        private static DocumentController GetLayoutDocumentForData(FieldModelController fieldModelController,
            DocumentController docController, Key key, Context context)
        {
            DocumentController layoutDocument = null;
            if (fieldModelController is TextFieldModelController)
            {
                layoutDocument = new TextingBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is NumberFieldModelController)
            {
                layoutDocument = new TextingBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                layoutDocument = new ImageBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            } else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                layoutDocument = new CollectionBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            } else if (fieldModelController is DocumentFieldModelController)
            {
                layoutDocument = new DocumentBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            }
            else if (fieldModelController is RichTextFieldModelController)
            {
                layoutDocument = new RichTextBox(new ReferenceFieldModelController(docController.GetId(), key)).Document;
            }
            return layoutDocument;
        }

        private SelectableContainer GetFirstCompositeLayoutContainer(Point dropPoint)
        {
            var elem = VisualTreeHelper.FindElementsInHostCoordinates(dropPoint, _editingDocView)
                .FirstOrDefault(AssertIsCompositeLayout);
            return elem as SelectableContainer;
        }

        private bool AssertIsCompositeLayout(object obj)
        {
            if (!(obj is SelectableContainer))
            {
                return false;
            }
            var cont = (SelectableContainer) obj;
            return this.IsCompositeLayout(cont.LayoutDocument);
        }
        
        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument, DocumentController dataDocument)
        {
            xSettingsPane.Children.Clear();
            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(layoutDocument, dataDocument);
            _selectedContainer = sender;
            if (newSettingsPane != null)
            {
                xSettingsPane.Children.Add(newSettingsPane);
            }
        }

        public bool IsCompositeLayout( DocumentController layoutDocument)
        {
            return layoutDocument.DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout ||
                   layoutDocument.DocumentType == GridViewLayout.DocumentType ||
                   layoutDocument.DocumentType == ListViewLayout.DocumentType;
        }
        
        private void BreadcrumbListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DocumentController cont = e.ClickedItem as DocumentController;

            SetUpInterfaceBuilder(cont, new Context(cont));
        }
        private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var item = e.Items.FirstOrDefault();
            if (item is Button)
            {
                //var defaultNewSize = new Size(400, 400);
                var button = item as Button;

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
            //if (_selectedContainer.ParentContainer != null)
            //{
            //    var collection =
            //        _selectedContainer.ParentContainer.LayoutDocument.GetField(DashConstants.KeyStore.DataKey) as
            //            DocumentCollectionFieldModelController;
            //    collection?.RemoveDocument(_selectedContainer.LayoutDocument);
            //    _selectedContainer.ParentContainer.SetSelectedContainer(null);
            //}

            throw new NotImplementedException();
        }
    }
}
