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
            SetActiveLayoutToFreeform_TEMP(docController);
            var docViewModel = new DocumentViewModel(docController, true);
            _documentView = new DocumentView(docViewModel);
            _documentController = docController;
            var rootSelectableContainer = _documentView.ViewModel.Content as SelectableContainer;
            rootSelectableContainer.OnSelectionChanged += RootSelectableContainerOnOnSelectionChanged;


            // set the middle pane to hold the document view
            xDocumentHolder.Child = _documentView;

            xKeyValuePane.SetDataContextToDocumentController(docController);

            _documentView.DragOver += DocumentViewOnDragOver;
            _documentView.Drop += DocumentViewOnDrop;
            _documentView.AllowDrop = true;
        }

        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument)
        {
            xSettingsPane.Children.Clear();
            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(layoutDocument);
            if (newSettingsPane != null)
            {
                xSettingsPane.Children.Add(newSettingsPane);
            }
        }

        private void SetActiveLayoutToFreeform_TEMP(DocumentController docController)
        {
            var currentDocPosition = docController.GetPositionField().Data;
            var defaultNewSize = new Size(400, 400);
            docController.SetActiveLayout(new FreeFormDocument(new List<DocumentController>(), currentDocPosition, defaultNewSize).Document, 
                forceMask: true, 
                addToLayoutList: true);
        }


        private void BreadcrumbListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DocumentController cont = e.ClickedItem as DocumentController;

            SetUpInterfaceBuilder(cont, new Context(cont));
        }

        private void DocumentViewOnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void DocumentViewOnDrop(object sender, DragEventArgs e)
        {
            // extract required information from drop event
            var context = new Context(_documentController);
            var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            var fieldModelController = _documentController.GetDereferencedField(key, context);
            var dropPointFMC = new PointFieldModelController(e.GetPosition(_documentView).X, e.GetPosition(_documentView).Y);

            // view factory
            CourtesyDocuments.CourtesyDocument box = null;
            if (fieldModelController is TextFieldModelController)
            {
                box = new TextingBox(new DocumentReferenceController(_documentController.GetId(), key));
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                box = new ImageBox(new DocumentReferenceController(_documentController.GetId(), key));
            }

            // safety check
            if (box == null)
            {
                return;
            }

            // drop factory???
            var activeLayout = _documentController.GetActiveLayout(context).Data;
            if (activeLayout.DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout)
            {
                box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, dropPointFMC, forceMask: true);
                var data =
                    activeLayout.GetField(DashConstants.KeyStore.DataKey) as DocumentCollectionFieldModelController;
                data?.AddDocument(box.Document);
            }



            //var _documentController = _layoutCourtesyDocument.Document;
            //var docController = _layoutCourtesyDocument.Document;
            //var context = new Context(docController);

            //var key = e.Data.Properties[KeyValuePane.DragPropertyKey] as Key;
            //var fieldModelController = docController.GetDereferencedField(key, null);
            //CourtesyDocuments.CourtesyDocument box = null;
            //if (fieldModelController is TextFieldModelController)
            //{
            //    var textFieldModelController = fieldModelController as TextFieldModelController;
            //    if (docController.GetPrototype() != null && docController.GetPrototype().GetDereferencedField(key, null) == null)
            //    {
            //        docController.GetPrototype().SetField(key, textFieldModelController, false);
            //    }

            //    var layoutDoc = (docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, null) as DocumentFieldModelController)?.Data;

            //    if (layoutDoc == null || !docController.IsDelegateOf(layoutDoc.GetId()))
            //        layoutDoc = docController;
            //    // bcz: hack -- the idea is that if we're dropping a field on a prototype layout, then the layout should reference the prototype of
            //    //       of the source document as well.  Otherwise, the other documents that use this prototype layout will get the data from this source document
            //    var layoutDocPrototype = layoutDoc.GetPrototype() == null ? layoutDoc : layoutDoc.GetPrototype();

            //    if (textFieldModelController.TextFieldModel.Data.EndsWith(".jpg"))
            //        box = new CourtesyDocuments.ImageBox(new DocumentReferenceController(layoutDocPrototype.GetId(), key));
            //    else box = new CourtesyDocuments.TextingBox(new DocumentReferenceController(layoutDocPrototype.GetId(), key));
            //}
            //else if (fieldModelController is ImageFieldModelController)
            //{
            //    box = new CourtesyDocuments.ImageBox(new DocumentReferenceController(docController.GetId(), key));
            //}
            //else if (fieldModelController is DocumentCollectionFieldModelController)
            //{
            //    box = new CourtesyDocuments.CollectionBox(new DocumentReferenceController(docController.GetId(), key));
            //}
            //else if (fieldModelController is NumberFieldModelController)
            //{
            //    box = new CourtesyDocuments.TextingBox(new DocumentReferenceController(docController.GetId(), key));
            //}
            //else if (fieldModelController is DocumentFieldModelController)
            //{
            //    box = new CourtesyDocuments.LayoutCourtesyDocument(ContentController.GetController<DocumentFieldModelController>(fieldModelController.GetId()).Data);
            //}

            //var layoutDocFieldController = docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context);
            //if (box != null)
            //{
            //    //Sets the point position of the image/text box
            //    var pfmc = new PointFieldModelController(e.GetPosition(_documentView).X, e.GetPosition(_documentView).Y);
            //    box.Document.SetField(DashConstants.KeyStore.PositionFieldKey, pfmc, false);

            //    var layoutDataField = _layoutCourtesyDocument.ActiveLayoutDocController?.GetDereferencedField(DashConstants.KeyStore.DataKey, null);

            //    if (layoutDataField is DocumentCollectionFieldModelController)
            //    {
            //        (layoutDataField as DocumentCollectionFieldModelController).AddDocument(box.Document);
            //    }
            //    else
            //    {
            //        var newLayoutCollection = new CollectionBox(new DocumentCollectionFieldModelController(new DocumentController[] { (docController.GetDereferencedField(DashConstants.KeyStore.ActiveLayoutKey, context) as DocumentFieldModelController).Data, box.Document }));
            //        var oldPt = ((layoutDocFieldController as DocumentFieldModelController).Data.GetDereferencedField(DashConstants.KeyStore.PositionFieldKey, null) as PointFieldModelController).Data;
            //        (layoutDocFieldController as DocumentFieldModelController).Data.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Windows.Foundation.Point()), false);
            //        layoutDocFieldController = new DocumentFieldModelController(newLayoutCollection.Document);
            //        (layoutDocFieldController as DocumentFieldModelController).Data.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(oldPt), false);
            //    }
            //}

            //ApplyEditable();
            //docController.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutDocFieldController, false);
        }



    }
}
