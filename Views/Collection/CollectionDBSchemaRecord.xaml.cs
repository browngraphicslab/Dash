using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
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
            args.Data.RequestedOperation = DataPackageOperation.Link;
            GetLayoutFromDataDocAndSetDefaultLayout(dataDoc);
        }

        // TODO lsm wrote this here it's a hack we probably want to do this in places other than the schema record
        private static DocumentController GetLayoutFromDataDocAndSetDefaultLayout(DocumentController dataDoc)
        {
            var isLayout = dataDoc.GetField(KeyStore.DocumentContextKey) != null;
            var layoutDocType = (dataDoc.GetField(KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data
                ?.DocumentType;
            if (!isLayout && (layoutDocType == null || layoutDocType.Equals(DefaultLayout.DocumentType)))
            {
                if (dataDoc.GetField(KeyStore.ThisKey) == null)
                    dataDoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dataDoc), true);
                var layoutDoc =
                    new KeyValueDocumentBox(new DocumentReferenceFieldController(dataDoc.GetId(), KeyStore.ThisKey));

                layoutDoc.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
                layoutDoc.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
                dataDoc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }

            return isLayout ? dataDoc : dataDoc.GetActiveLayout(null).Data;
        }
    }

    /// <summary>
    /// View model to represent a single document (record) in a schema view
    /// </summary>
    public class CollectionDBSchemaRecordViewModel
    {
        /// <summary>
        /// Document containing the collection this record is in, this is the DataDocument
        /// </summary>
        public DocumentController ParentDoc { get; }

        /// <summary>
        /// The backing document for this record
        /// </summary>
        public DocumentController Document { get; }

        /// <summary>
        /// All the different fields on this record
        /// </summary>
        public ObservableCollection<CollectionDBSchemaRecordFieldViewModel> RecordFields { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDoc">The document which contains this list of records, basically the doc containing the collection this record is a part of</param>
        /// <param name="document">The document that this record is going to represent (think of the document as a row in a database table)</param>
        /// <param name="fields">List of view models for fields that are in this row (think cell in a database table)</param>
        public CollectionDBSchemaRecordViewModel(DocumentController parentDoc, DocumentController document, IEnumerable<CollectionDBSchemaRecordFieldViewModel> fields)
        {
            ParentDoc = parentDoc;
            Document = document;
            RecordFields = new ObservableCollection<CollectionDBSchemaRecordFieldViewModel>(fields);
        }

    }
}
