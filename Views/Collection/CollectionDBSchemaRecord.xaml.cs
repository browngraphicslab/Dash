using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaRecord : UserControl
    {

        private PointerPoint _downPt;
        private DocumentController _dataContextDocument;


        public CollectionDBSchemaRecord()
        {
            this.InitializeComponent();
        }

        private void CollectionDBSchemaRecordField_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var parent = this.GetFirstAncestorOfType<CollectionDBSchemaView>();
                //parent.xRecordsView.SelectedItem = this.DataContext;
                _downPt = e.GetCurrentPoint(null);
                e.Handled = true;
            }
            else
                _downPt = null;
        }

        private async void CollectionDBSchemaRecordField_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_downPt == null) return;

            e.Complete();
            await StartDragAsync(_downPt);
            e.Handled = true;
        }

        private void UserControl_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            DocumentController dataDoc = (DataContext as CollectionDBSchemaRecordViewModel)?.Document;
            args.Data.SetDragModel(new DragDocumentModel(dataDoc));
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;

            dataDoc.GetLayoutFromDataDocAndSetDefaultLayout();
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
        public ObservableCollection<EditableScriptViewModel> RecordFields { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentDoc">The document which contains this list of records, basically the doc containing the collection this record is a part of</param>
        /// <param name="document">The document that this record is going to represent (think of the document as a row in a database table)</param>
        /// <param name="fields">List of view models for fields that are in this row (think cell in a database table)</param>
        public CollectionDBSchemaRecordViewModel(DocumentController parentDoc, DocumentController document, IEnumerable<EditableScriptViewModel> fields)
        {
            ParentDoc = parentDoc;
            Document = document;
            RecordFields = new ObservableCollection<EditableScriptViewModel>(fields);
        }

    }
}
