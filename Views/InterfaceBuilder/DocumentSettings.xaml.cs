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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class DocumentSettings : UserControl
    {
        public DocumentSettings()
        {
            this.InitializeComponent();
        }

        public DocumentSettings(DocumentController editedLayoutDocument, Context context): this()
        {
            xSizeRow.Children.Add(new SizeSettings(editedLayoutDocument, context));
            xPositionRow.Children.Add(new PositionSettings(editedLayoutDocument, context));

            xAddLayoutButton.Tapped += CreateNewActiveLayout;

            SetupActiveLayoutComboBox(editedLayoutDocument, context);
        }

        private void SetupActiveLayoutComboBox(DocumentController editedLayoutDocument, Context context)
        {
            var layoutList = editedLayoutDocument.GetLayoutList(context);
            layoutList.OnDocumentsChanged += LayoutList_OnDocumentsChanged;
            SetActiveLayoutComboBoxItems(layoutList.GetDocuments());
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
            throw new NotImplementedException();
        }
    }
}
