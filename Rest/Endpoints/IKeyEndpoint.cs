using System;
using DashShared;

namespace Dash
{
    public interface IKeyEndpoint
    {
        /// <summary>
        /// </summary>
        /// <param name="newKey"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error);

        /// <summary>
        /// </summary>
        /// <param name="keyToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error);

        void GetKey(string id, Action<KeyModel> success, Action<Exception> error);
        void DeleteKey(KeyModel keyToDelete, Action success, Action<Exception> error);
    }
}