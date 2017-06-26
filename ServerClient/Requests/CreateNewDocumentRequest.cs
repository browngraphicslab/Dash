using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class CreateNewDocumentRequest : Request
    {
        private readonly string _newDocumentId;

        public CreateNewDocumentRequest(CreateNewDocumentRequestArgs newDocumentRequestArgs)
        {
            // create controllers and store models for each of the field models if we have to
            foreach (var kvp in newDocumentRequestArgs.Fields)
            {
                var key = kvp.Key;
                var fieldModel = kvp.Value;

                if (!ContentController.HasModel(fieldModel.Id))
                {
                    ContentController.AddModel(fieldModel);
                }
                if (!ContentController.HasController(fieldModel.Id))
                {
                    var fieldModelController = FieldModelControllerFactory.CreateFromModel(fieldModel);
                    ContentController.AddController(fieldModelController);
                }
            }

            // create the doucment model and document controller and add it to the content controller
            var documentModel = new DocumentModel(newDocumentRequestArgs.Fields, newDocumentRequestArgs.Type);
            ContentController.AddModel(documentModel);
            var documentController = new DocumentController(documentModel);
            ContentController.AddController(documentController);

            _newDocumentId = documentModel.Id;
        }

        public DocumentController GetReturnedDocumentController()
        {
            return ContentController.GetController<DocumentController>(_newDocumentId);
        }

        public DocumentModel GetReturnedDocumentModel()
        {
            return ContentController.GetModel<DocumentModel>(_newDocumentId);
        }
    }
}
