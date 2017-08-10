using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public class DropControls
    {
        private FrameworkElement _dropTarget;
        private DocumentController _layoutDocument;

        public DropControls(FrameworkElement dropTarget, DocumentController layoutDocument)
        {
            _dropTarget = dropTarget;
            _layoutDocument = layoutDocument;
            _dropTarget.Drop += dropTargetOnDrop;
            _dropTarget.DragOver += _dropTarget_DragOver;
            _dropTarget.AllowDrop = true;
        }

        private void _dropTarget_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void dropTargetOnDrop(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            var kvp = (KeyValuePair<KeyController, DocumentController>)e.Data.Properties[KeyValuePane.DragPropertyKey];
            var docController = kvp.Value;
            var key = kvp.Key;
            var context = new Context(docController);
            var fieldModelController = docController.GetDereferencedField(key, context);
            var dropPointFMC = new PointFieldModelController(e.GetPosition(_dropTarget).X, e.GetPosition(_dropTarget).Y);

            // view factory
            CourtesyDocument box = null;
            if (fieldModelController is TextFieldModelController)
            {
                box = new TextingBox(new ReferenceFieldModelController(docController.GetId(), key));
            }
            else if (fieldModelController is ImageFieldModelController)
            {
                box = new ImageBox(new ReferenceFieldModelController(docController.GetId(), key));
            }

            // safety check
            if (box == null)
            {
                return;
            }

            // drop factory???
            if (_layoutDocument.DocumentType == DashConstants.TypeStore.FreeFormDocumentLayout)
            {
                box.Document.SetField(KeyStore.PositionFieldKey, dropPointFMC, forceMask: true);
            }
            var data =
                _layoutDocument.GetField(KeyStore.DataKey) as DocumentCollectionFieldModelController;
            data?.AddDocument(box.Document);
        }
    }
}
