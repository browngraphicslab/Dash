namespace Dash
{
    public class CollectionModel
    {
        public DocumentCollectionFieldModel DocumentCollectionFieldModel;
        public DocumentController Context;

        public CollectionModel(DocumentCollectionFieldModel docCollectionFieldModel, DocumentController context)
        {
            DocumentCollectionFieldModel = docCollectionFieldModel;
            Context = context;
        }

    }
}
