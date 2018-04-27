using System.Collections.Generic;
using System.Linq;

namespace DashShared
{
    public class RestRequestReturnArgs : EntityBase
    {
        public IEnumerable<EntityBase> ReturnedObjects;

        public RestRequestReturnArgs()
        {
            
        }

        public RestRequestReturnArgs(IEnumerable<EntityBase>  returnedObjects)
        {
            ReturnedObjects = returnedObjects;
        }

        public RestRequestReturnArgs(IEnumerable<RestRequestReturnArgs> args)
        {
            ReturnedObjects = args.SelectMany(arg => arg.ReturnedObjects);
        }

        /*
        public IEnumerable<DocumentModel> DocumentModels;
        public IEnumerable<FieldModel> FieldModels;
        public IEnumerable<KeyModel> KeyModels;*/
    }
}
