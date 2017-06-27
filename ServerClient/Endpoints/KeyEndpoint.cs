using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class KeyEndpoint
    {
        #region RemovedFakeLocal
        private Dictionary<string, Key> _keys;

        private int _numKeys;
        #endregion

        public KeyEndpoint()
        {
            _keys = new Dictionary<string, Key>();
        }

        public Key GetKeyAsync(string keyId)
        {
            return _keys.ContainsKey(keyId) ? _keys[keyId] : null;
        }

        public void DeleteKeyAsync(Key model)
        {
            _keys.Remove(model.Id);
        }

        public Key UpdateKeyAsync(Key model)
        {
            _keys[model.Id] = model;
            return model;
        }

        public Key CreateKeyAsync(string name)
        {
            var id = name; // $"{_numKeys++}";

            var newKey = new Key
            {
                Id = id,
                Name = name
            };

            _keys[id] = newKey;

            return newKey;
        }
    }
}