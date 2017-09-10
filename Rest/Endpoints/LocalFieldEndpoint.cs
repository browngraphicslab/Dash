using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class LocalFieldEndpoint : IFieldEndpoint
    {
        public void AddField(FieldModel newField, Action<FieldModelDTO> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public void UpdateField(FieldModel fieldToUpdate, Action<FieldModelDTO> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task GetField(string id, Action<FieldModelDTO> success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
        {
            throw new NotImplementedException();
        }
    }
}
