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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class CollectionDBSchemaRecord : UserControl
    {
        static int count = 0;
        public CollectionDBSchemaRecord()
        {
            count++;
            // Debug.WriteLine("Created " + count);
            this.InitializeComponent();
        }
        

        PointerPoint _downPt;

        private void CollectionDBSchemaRecordField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _downPt = e.GetCurrentPoint(null);
            e.Handled = true;
        }

        private void CollectionDBSchemaRecordField_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void CollectionDBSchemaRecordField_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Complete();
            var val= this.StartDragAsync(_downPt);
            e.Handled = true;
        }

        private void UserControl_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dataDoc = (DataContext as CollectionDBSchemaRecordViewModel).Document;
            args.Data.Properties.Add("DocumentControllerList", new List<DocumentController>(new DocumentController[] { dataDoc }));
            args.Data.Properties.Add("View", true);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Link;
            if ((dataDoc.GetField(KeyStore.ActiveLayoutKey) as DocumentFieldModelController)?.Data?.DocumentType == DefaultLayout.DocumentType)
            {
                if (dataDoc.GetField(KeyStore.ThisKey) == null)
                    dataDoc.SetField(KeyStore.ThisKey, new DocumentFieldModelController(dataDoc), true);
                var layoutDoc = new KeyValueDocumentBox(new ReferenceFieldModelController(dataDoc.GetId(), KeyStore.ThisKey));

                layoutDoc.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(300), true);
                layoutDoc.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(100), true);
                dataDoc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }
        }
    }

    public class CollectionDBSchemaRecordViewModel
    {
        public CollectionDBSchemaRecordViewModel(DocumentController document, IEnumerable<CollectionDBSchemaRecordFieldViewModel> fields)
        {
            Document = document;
            RecordFields = new ObservableCollection<Views.CollectionDBSchemaRecordFieldViewModel>(fields);
        }
        public DocumentController Document;
        public ObservableCollection<CollectionDBSchemaRecordFieldViewModel> RecordFields { get; set; }
    }
}
