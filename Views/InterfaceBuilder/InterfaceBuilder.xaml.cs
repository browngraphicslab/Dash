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

        private DropControls _controls;

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
            _documentController = docController;
            var rootSelectableContainer = _documentView.ViewModel.Content as SelectableContainer;
            rootSelectableContainer.OnSelectionChanged += RootSelectableContainerOnOnSelectionChanged;


            // set the middle pane to hold the document view
            xDocumentHolder.Child = _documentView;

            xKeyValuePane.SetDataContextToDocumentController(docController);
        }

        private void RootSelectableContainerOnOnSelectionChanged(SelectableContainer sender, DocumentController layoutDocument)
        {
            xSettingsPane.Children.Clear();
            var newSettingsPane = SettingsPaneFromDocumentControllerFactory.CreateSettingsPane(layoutDocument);
            if (newSettingsPane != null)
            {
                xSettingsPane.Children.Add(newSettingsPane);
            }
            if (layoutDocument.DocumentType == DashConstants.DocumentTypeStore.FreeFormDocumentLayout || layoutDocument.DocumentType == GridViewLayout.DocumentType)
            {
                _controls = new DropControls(sender, layoutDocument);
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

        private void SetActiveLayoutToGridView_TEMP(DocumentController docController)
        {
            var currentDocPosition = docController.GetPositionField().Data;
            var defaultNewSize = new Size(400, 400);
            docController.SetActiveLayout(new GridViewLayout(new List<DocumentController>(), currentDocPosition, defaultNewSize).Document,
                forceMask: true,
                addToLayoutList: true);
        }

        private void BreadcrumbListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            DocumentController cont = e.ClickedItem as DocumentController;

            SetUpInterfaceBuilder(cont, new Context(cont));
        }
    }
}
