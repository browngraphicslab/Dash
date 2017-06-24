using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Dash
{
    public class DocumentCollectionFieldModelController : FieldModelController
    {
        /// <summary>
        ///     A wrapper for <see cref="DocumentCollectionFieldModel.Data" />. Change this to propogate changes
        ///     to the server and across the client
        /// </summary>
        public ObservableCollection<DocumentController> Documents;

        /// <summary>
        ///     Create a new <see cref="DocumentCollectionFieldModelController" /> associated with the passed in
        ///     <see cref="DocumentCollectionFieldModel" />
        /// </summary>
        /// <param name="documentCollectionFieldModel">The model which this controller will be operating over</param>
        public DocumentCollectionFieldModelController(DocumentCollectionFieldModel documentCollectionFieldModel)
            : base(documentCollectionFieldModel)
        {
            // Initialize Local Variables
            DocumentCollectionFieldModel = documentCollectionFieldModel;
            var documentControllers =
                ContentController.GetControllers<DocumentController>(documentCollectionFieldModel.Data);
            Documents = new ObservableCollection<DocumentController>(documentControllers);

            // Add Events
            Documents.CollectionChanged += DocumentsCollectionChanged;
        }

        /// <summary>
        ///     The <see cref="DocumentCollectionFieldModel" /> associated with this
        ///     <see cref="DocumentCollectionFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentCollectionFieldModel DocumentCollectionFieldModel { get; }

        /// <summary>
        /// Called whenver the Data in <see cref="Documents"/> changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //// we could fine tune this
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Add:
            //        break;
            //    case NotifyCollectionChangedAction.Move:
            //        break;
            //    case NotifyCollectionChangedAction.Remove:
            //        break;
            //    case NotifyCollectionChangedAction.Replace:
            //        break;
            //    case NotifyCollectionChangedAction.Reset:
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            var freshList = sender as ObservableCollection<DocumentController>;
            Debug.Assert(freshList != null);
            DocumentCollectionFieldModel.Data = freshList.Select(documentController => documentController.GetId());

            // Update Local
            // Update Server
        }
    }
}