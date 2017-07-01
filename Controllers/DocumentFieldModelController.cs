namespace Dash
{
    public class DocumentFieldModelController : FieldModelController
    {
        /// <summary>
        ///     Create a new DocumentFieldModelController associated with the passed in <see cref="DocumentModelFieldModel" />
        /// </summary>
        /// <param name="documentFieldModel">The model which this controller will be operating over</param>
        public DocumentFieldModelController(DocumentModelFieldModel documentFieldModel) : base(documentFieldModel)
        {
            DocumentModelFieldModel = documentFieldModel;
        }

        /// <summary>
        ///     The <see cref="DocumentModelFieldModel" /> associated with this <see cref="DocumentFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public DocumentModelFieldModel DocumentModelFieldModel { get; }

        /// <summary>
        ///     A wrapper for <see cref="DocumentModelFieldModel.Data" />. Change this to propagate changes
        ///     to the server
        /// </summary>
        public DocumentController Data
        {
            get { return ContentController.GetController<DocumentController>(DocumentModelFieldModel.Data.Id); }
            set
            {
                if (SetProperty(ref DocumentModelFieldModel.Data, value.DocumentModel))
                {
                    // update local
                    // update server
                }
            }
        }
    }
}