using DashShared;

namespace Dash
{
    public class KeyControllerFactory : BaseControllerFactory
    {
        public static KeyController CreateFromModel(KeyModel model)
        {
            return new KeyController(model, false);
        }
    }
}
