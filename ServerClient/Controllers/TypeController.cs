using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class TypeController
    {
        #region RemovedFakeLocal
        private Dictionary<string, DocumentType> _types;

        private int _numTypes;

        #endregion

        public TypeController()
        {
            _types = new Dictionary<string, DocumentType>();
        }

        public DocumentType GetTypeAsync(string typeId)
        {
            return _types[typeId];
        }

        public void DeleteTypeAsync(DocumentType model)
        {
            _types.Remove(model.Id);
        }

        public DocumentType UpdateTypeAsync(DocumentType model)
        {
            _types[model.Id] = model;
            return model;
        }

        public DocumentType CreateTypeAsync(string type)
        {
            var id = $"{_numTypes++}";

            var newType = new DocumentType()
            {
                Id = id,
                Type = type
            };

            _types[id] = newType;

            return newType;
        }
    }
}