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
        public string OperationBarDragKey = "4D9172C1-266F-4119-BB76-961D7D6C37B0";
        private SimpleCollectionViewModel _collectionViewModel;

        public CompoundOperatorEditor()
        {
            this.InitializeComponent();
            Unloaded += CompoundOperatorEditor_Unloaded;
            _operatorList = new ObservableCollection<OperatorBuilder>(OperationCreationHelper.Operators.Values);
            _collectionViewModel = new SimpleCollectionViewModel(false);
            xFreeFormEditor.DataContext = _collectionViewModel;
        }

        public CompoundOperatorEditor(DocumentController documentController, CompoundOperatorFieldController operatorFieldModelController) : this()
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
                e.Data.Properties.Add(OperationBarDragKey, item);
            }
        }


        private void XOperationListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private void XFreeFormEditor_OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void XFreeFormEditor_OnDrop(object sender, DragEventArgs e)
        {
            var isDraggedFromOperationsBar = e.Data.Properties[OperationBarDragKey] != null;

            if (isDraggedFromOperationsBar)
            {
                var opBuilder = e.Data.Properties[OperationBarDragKey] as OperatorBuilder;

                var pos = Util.GetCollectionFreeFormPoint(xFreeFormEditor, e.GetPosition(MainPage.Instance));

                var opDoc = opBuilder.OperationDocumentConstructor.Invoke();

                opDoc.GetPositionField(null).Data = new Point(pos.X, pos.Y);

                _collectionViewModel.AddDocuments(new List<DocumentController>{opDoc}, null);


            }
            
        }
    }
}
