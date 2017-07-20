using System;
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
using System;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        /// <summary>
        /// The document view of the document which is being edited
        /// </summary>
        private DocumentView _documentView;
        private DocumentController _documentController;
        public static string LayoutDragKey = "B3B49D46-6D56-4CC9-889D-4923805F2DA9";
        public enum DisplayTypeEnum { List, Grid, Freeform } 

        private DisplayTypeEnum _display = DisplayTypeEnum.Freeform;  

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

        private void SetUpInterfaceBuilder(DocumentController docController, Context context)
        {
            SetActiveLayoutToGridView_TEMP(docController);
            var docViewModel = new DocumentViewModel(docController, true);
            _documentView = new DocumentView(docViewModel);
            _documentView.Manipulator.RemoveAllButHandle();
            _documentView.RemoveScroll();
            _documentController = docController;
            var rootSelectableContainer = _documentView.ViewModel.Content as SelectableContainer;
            rootSelectableContainer.OnSelectionChanged += RootSelectableContainerOnOnSelectionChanged;

            _documentView.DragOver += DocumentViewOnDragOver;
            _documentView.AllowDrop = true;
            _documentView.Drop += DocumentViewOnDrop;


            // set the middle pane to hold the document view
            xDocumentHolder.Child = _documentView;

            xKeyValuePane.SetDataContextToDocumentController(docController);
        }

<<<<<<< HEAD
        private void DocumentViewOnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {
            var layoutContainer = GetFirstCompositeLayoutContainer(e.GetPosition(MainPage.Instance));
            if (layoutContainer != null)
            {
                if (e.Data.Properties[KeyValuePane.DragPropertyKey] != null)
                {
                    var kvp = (KeyValuePair<Key, DocumentController>)e.Data.Properties[KeyValuePane.DragPropertyKey];
                    var docController = kvp.Value;
                    var key = kvp.Key;
                    var context = new Context(docController);
                    var fieldModelController = docController.GetDereferencedField(key, context);
                    var dropPointFMC = new PointFieldModelController(e.GetPosition(_documentView).X, e.GetPosition(_documentView).Y);

                    // view factory
                    CourtesyDocuments.CourtesyDocument box = null;
                    if (fieldModelController is TextFieldModelController)
                    {
                        box = new CourtesyDocuments.TextingBox(new DocumentReferenceController(docController.GetId(), key));
                    }
                    else if (fieldModelController is ImageFieldModelController)
                    {
                        box = new CourtesyDocuments.ImageBox(new DocumentReferenceController(docController.GetId(), key));
                    }

                    // safety check
                    if (box == null)
                    {
                        return;
                    }

                    // drop factory???
                    if (layoutContainer.LayoutDocument.DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
                    {
                        box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, dropPointFMC, forceMask: true);
                    }
                    var data =
                        layoutContainer.LayoutDocument.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
                    data?.AddDocument(box.Document);
                } else if (e.Data.Properties[LayoutDragKey] != null && e.Data.Properties[LayoutDragKey] is DisplayTypeEnum)
                {
                    var displayType = (DisplayTypeEnum) e.Data.Properties[LayoutDragKey];
                    switch (displayType)
                    {
                        case DisplayTypeEnum.Freeform:
                            break;
                        case DisplayTypeEnum.Grid:
                            break;
                        case DisplayTypeEnum.List:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private SelectableContainer GetFirstCompositeLayoutContainer(Point dropPoint)
        {
            var elem = VisualTreeHelper.FindElementsInHostCoordinates(dropPoint, _documentView)
                .First(AssertIsCompositeLayout);
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

        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument)
=======
        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument, DocumentController dataDocument)
>>>>>>> 3ab96aa2b3805a1afb5ff8bbd68863c0b97d3009
        {
            xSettingsPane.Children.Clear();
            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(layoutDocument, dataDocument);
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

        private void SetActiveLayoutToFreeform_TEMP(DocumentController docController)
        {
            var currentDocPosition = docController.GetPositionField().Data;
            var defaultNewSize = new Size(400, 400);
            docController.SetActiveLayout(
                new FreeFormDocument(new List<DocumentController>(), currentDocPosition, defaultNewSize).Document,
                forceMask: true,
                addToLayoutList: true);
        }

        private void SetActiveLayoutToGridView_TEMP(DocumentController docController)
        {
            var currentDocPosition = docController.GetPositionField().Data;
            var defaultNewSize = new Size(400, 400);
            docController.SetActiveLayout(
                new GridViewLayout(new List<DocumentController>(), currentDocPosition, defaultNewSize).Document,
                forceMask: true,
                addToLayoutList: true);
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
                var defaultNewSize = new Size(400, 400);
                var button = item as Button;
                switch (button.Content)
                {
                    case "🖹":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.List;
                        break;
                    case "▦":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.Grid;
                        break;
                    case "⊡":
                        e.Data.Properties[LayoutDragKey] = DisplayTypeEnum.Freeform;
                        break;
                    default:
                        break;
                }
                e.Data.RequestedOperation = DataPackageOperation.Move;
            }
        }
    }
}
