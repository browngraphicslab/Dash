using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CompoundOperatorEditor : UserControl
    {
        private readonly DocumentController _documentController;
        private readonly CompoundOperatorFieldController _operatorFieldModelController;
        private ObservableCollection<OperatorBuilder> _operatorList;
        private CollectionViewModel _collectionViewModel;

        public CompoundOperatorEditor(FieldControllerBase collectionField, Context context = null)
        {
            this.InitializeComponent();
            Unloaded += CompoundOperatorEditor_Unloaded;
            _operatorList = new ObservableCollection<OperatorBuilder>(OperationCreationHelper.Operators.Values);
            _collectionViewModel = new CollectionViewModel(collectionField, context: context);
            xFreeFormEditor.DataContext = _collectionViewModel;
        }

        public CompoundOperatorEditor(DocumentController documentController, CompoundOperatorFieldController operatorFieldModelController, FieldControllerBase collectionField , Context context = null) : this(collectionField, context)
        {
            _documentController = documentController;
            _operatorFieldModelController = operatorFieldModelController;
        }

        private void CompoundOperatorEditor_Unloaded(object sender, RoutedEventArgs e)
        {
        }


        private void XOperationListView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var item = e.Items.FirstOrDefault();

            // item type has to be the same as ListItemSource item type
            if (item is OperatorBuilder)
            {
                e.Data.RequestedOperation = DataPackageOperation.Move;
                e.Data.Properties.Add(CompoundOperatorFieldController.OperationBarDragKey, item);
            }

           // ItemsCarrier.Instance.SourceCollection = _collectionViewModel; 
        }
        
        private void XFreeFormEditor_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.Handled = true;
        }

        private void XFreeFormEditor_OnDrop(object sender, DragEventArgs e)
        { 
            if (e.Data == null) return; 
            var isDraggedFromOperationsBar = e.Data.Properties[CompoundOperatorFieldController.OperationBarDragKey] != null;

            if (isDraggedFromOperationsBar)
            {
                var opBuilder = e.Data.Properties[CompoundOperatorFieldController.OperationBarDragKey] as OperatorBuilder;

                var pos = Util.GetCollectionFreeFormPoint(xFreeFormEditor, e.GetPosition(MainPage.Instance));

                var opDoc = opBuilder.OperationDocumentConstructor.Invoke();

                opDoc.GetPositionField(null).Data = new Point(pos.X, pos.Y);

                _collectionViewModel.AddDocuments(new List<DocumentController>{opDoc}, null);

                e.Data.Properties[CompoundOperatorFieldController.OperationBarDragKey] = null; 
            } 
        }
    }
}
