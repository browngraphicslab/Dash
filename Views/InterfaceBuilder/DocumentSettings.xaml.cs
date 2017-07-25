using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentSettings : UserControl
    {
        private readonly DocumentController _dataDocument;
        private readonly Context _context;

        public DocumentSettings()
        {
            this.InitializeComponent();
        }

        public DocumentSettings(DocumentController layoutDocument, DocumentController dataDocument, Context context): this()
        {
            _context = context;
            _dataDocument = dataDocument;

            SetupActiveLayoutComboBox(dataDocument, context);

            xAddLayoutButton.Tapped += CreateNewActiveLayout_TEMP;
        }

        private void SetupActiveLayoutComboBox(DocumentController dataDocument, Context context)
        {
            // listen to when the layout list changes
            var layoutList = dataDocument.GetLayoutList(context);
            layoutList.OnDocumentsChanged += LayoutList_OnDocumentsChanged;
            SetActiveLayoutComboBoxItems(layoutList.GetDocuments());

            // listen to when the active layout changes
            var activeLayout = dataDocument.GetActiveLayout(context).Data;
            dataDocument.AddFieldUpdatedListener(DashConstants.KeyStore.ActiveLayoutKey, DataDocument_DocumentFieldUpdated);
            SetComboBoxSelectedItem(activeLayout);

            xActiveLayoutComboBox.SelectionChanged += XActiveLayoutComboBox_OnSelectionChanged;
        }

        private void XActiveLayoutComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedLayoutDocument = xActiveLayoutComboBox.SelectedItem as DocumentController;
            var currLayoutDocument = _dataDocument.GetActiveLayout(_context).Data;
            if (currLayoutDocument.Equals(selectedLayoutDocument)  || selectedLayoutDocument == null)
            {
                return;
            }

            _dataDocument.SetActiveLayout(selectedLayoutDocument, true, false);
        }

        private void DataDocument_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var newLayout = (args.NewValue as DocumentFieldModelController).Data;
            SetComboBoxSelectedItem(newLayout);
        }

        private void SetComboBoxSelectedItem(DocumentController newLayout)
        {
            xActiveLayoutComboBox.SelectedItem = newLayout;
        }

        private void SetActiveLayoutComboBoxItems(IEnumerable<DocumentController> documents)
        {
            xActiveLayoutComboBox.ItemsSource = documents;
        }

        private void LayoutList_OnDocumentsChanged(IEnumerable<DocumentController> currentDocuments)
        {
            SetActiveLayoutComboBoxItems(currentDocuments);
        }

        // to be rewritten this just cycles through our possible layouts for documents
        private void CreateNewActiveLayout_TEMP(object sender, TappedRoutedEventArgs e)
        {
            var currActiveLayout = _dataDocument.GetActiveLayout(_context).Data;
            var currPos = currActiveLayout.GetPositionField(_context).Data;
            var currWidth = currActiveLayout.GetWidthField(_context).Data;
            var currHeight = currActiveLayout.GetHeightField(_context).Data;
            DocumentController newLayout = null;
            if (currActiveLayout.DocumentType.Equals(DashConstants.DocumentTypeStore.FreeFormDocumentLayout))
            {
                newLayout = new GridViewLayout(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }
            else if (currActiveLayout.DocumentType.Equals(GridViewLayout.DocumentType))
            {
                newLayout = new ListViewLayout(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }
            else
            {
                newLayout = new FreeFormDocument(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }

            _dataDocument.SetActiveLayout(newLayout, true, true);

            // get docs which have an active layout which was a delegate of the prev active layout
            var delegateDocs = _dataDocument.GetDelegates().GetDocuments()
                .Where(dc => dc.GetActiveLayout().Data.IsDelegateOf(currActiveLayout.GetId()));

            foreach (var dataDocDelegate in delegateDocs)
            {
                var dataDocLayout = dataDocDelegate.GetActiveLayout().Data;
                var dataDocPos = dataDocLayout.GetPositionField().Data;
                var dataDocWidth = dataDocLayout.GetWidthField().Data;
                var dataDocHeight = dataDocLayout.GetHeightField().Data;
                var delegateNewLayout = newLayout.MakeDelegate();
                var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(dataDocPos, new Size(dataDocWidth, dataDocHeight));
                delegateNewLayout.SetFields(defaultLayoutFields, true);
                dataDocDelegate.SetActiveLayout(delegateNewLayout, true, false);
            }

        }
    }
}
