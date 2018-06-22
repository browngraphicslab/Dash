using DashShared;

namespace Dash
{
    public class DocumentControllerFactory : BaseControllerFactory
    {
        public static DocumentController CreateFromModel(DocumentModel model)
        {
            return new DocumentController(model);
        }
    }
}
