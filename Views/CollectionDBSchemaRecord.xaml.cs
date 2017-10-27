using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Controllers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaRecord : UserControl
    {
        static int count = 0;

        private PointerPoint _downPt;


        public CollectionDBSchemaRecord()
        {
            count++;
            Debug.WriteLine("Created " + count);
            this.InitializeComponent();
        }
        
        private void CollectionDBSchemaRecordField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _downPt = e.GetCurrentPoint(null);
            e.Handled = true;
        }

        private void CollectionDBSchemaRecordField_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
            StartDragAsync(_downPt);
            e.Handled = true;
        }

        private void UserControl_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dataDoc = (DataContext as CollectionDBSchemaRecordViewModel).Document;
            args.Data.Properties.Add("DocumentControllerList", new List<DocumentController>(new DocumentController[] { dataDoc }));
            args.Data.Properties.Add("View", true);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
            var layoutDocType = (dataDoc.GetField(KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data?.DocumentType;
            if (layoutDocType == null || layoutDocType.Equals( DefaultLayout.DocumentType))
            {
                if (dataDoc.GetField(KeyStore.ThisKey) == null)
                    dataDoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dataDoc), true);
                var layoutDoc = new KeyValueDocumentBox(new DocumentReferenceFieldController(dataDoc.GetId(), KeyStore.ThisKey));

                layoutDoc.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
                layoutDoc.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
                dataDoc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }
        }
    }

    /// <summary>
    /// View model to represent a single document (record) in a schema view
    /// </summary>
    public class CollectionDBSchemaRecordViewModel
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="document">The document that this record is going to represent (think of the document as a row in a database table)</param>
        /// <param name="fields">List of view models for fields that are in this row (think cell in a database table)</param>
        public CollectionDBSchemaRecordViewModel(DocumentController document, IEnumerable<CollectionDBSchemaRecordFieldViewModel> fields)
        {
            Document = document;
            RecordFields = new ObservableCollection<CollectionDBSchemaRecordFieldViewModel>(fields);
        }
        public DocumentController Document;
        public ObservableCollection<CollectionDBSchemaRecordFieldViewModel> RecordFields { get; set; }
    }
}
