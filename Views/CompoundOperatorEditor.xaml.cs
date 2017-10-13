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

        private SimpleCollectionViewModel _collectionViewModel;

        public CompoundOperatorEditor()
        {
            this.InitializeComponent();
            _collectionViewModel = new SimpleCollectionViewModel(false);
            xFreeFormEditor.DataContext = _collectionViewModel;
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
