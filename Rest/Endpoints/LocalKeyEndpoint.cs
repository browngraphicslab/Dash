using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class LocalKeyEndpoint : IKeyEndpoint
    {
        public void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void GetKey(string id, Action<KeyModel> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void DeleteKey(KeyModel keyToDelete, Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }
    }
}
