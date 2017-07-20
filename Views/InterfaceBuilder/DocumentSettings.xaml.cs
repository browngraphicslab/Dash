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

namespace Dash.Views
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
            xSizeRow.Children.Add(new SizeSettings(layoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(layoutDocument, context));

            xAddLayoutButton.Tapped += CreateNewActiveLayout;

            SetupActiveLayoutComboBox(dataDocument, context);
        }

        private void SetupActiveLayoutComboBox(DocumentController dataDocument, Context context)
        {
            var layoutList = dataDocument.GetLayoutList(context);
            layoutList.OnDocumentsChanged += LayoutList_OnDocumentsChanged;
            SetActiveLayoutComboBoxItems(layoutList.GetDocuments());

            var activeLayout = dataDocument.GetActiveLayout(context).Data;
            dataDocument.AddFieldUpdatedListener(DashConstants.KeyStore.ActiveLayoutKey, DataDocument_DocumentFieldUpdated);

            SetComboBoxSelectedItem(activeLayout);
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

        private void CreateNewActiveLayout(object sender, TappedRoutedEventArgs e)
        {
            var currActiveLayout = _dataDocument.GetActiveLayout(_context).Data;
            var currPos = currActiveLayout.GetPositionField(_context).Data;
            var currWidth = currActiveLayout.GetWidthField(_context).Data;
            var currHeight = currActiveLayout.GetHeightField(_context).Data;
            DocumentController newLayout = null;
            if (currActiveLayout.DocumentType.Equals(DashConstants.DocumentTypeStore.FreeFormDocumentLayout))
            {
                newLayout = new CourtesyDocuments.GridViewLayout(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }
            if (currActiveLayout.DocumentType.Equals(CourtesyDocuments.GridViewLayout.DocumentType))
            {
                newLayout = new CourtesyDocuments.ListViewLayout(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }

            if (currActiveLayout.DocumentType.Equals(CourtesyDocuments.ListViewLayout.DocumentType))
            {
                newLayout = new CourtesyDocuments.FreeFormDocument(new List<DocumentController>(), currPos,
                    new Size(currWidth, currHeight)).Document;
            }

            _dataDocument.SetActiveLayout(newLayout, true, true);
        }
    }
}
