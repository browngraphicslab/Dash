using System.Collections.Generic;
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
        public List<DocumentController> Documents;

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
            Documents = new List<DocumentController>(documentControllers);

            // Add Events

        }

        /// <summary>
        ///     The <see cref="DocumentCollectionFieldModel" /> associated with this
        ///     <see cref="DocumentCollectionFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentCollectionFieldModel DocumentCollectionFieldModel { get; }

        /// <summary>
        /// Adds a single document to the collection.
        /// </summary>
        /// <param name="docController"></param>
        public void AddDocument(DocumentController docController)
        {

            Documents.Add(docController);
            DocumentCollectionFieldModel.Data = Documents.Select((d) => d.GetId());

            OnDataUpdated();
        }
    }
}