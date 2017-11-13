using System;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public interface IFieldEndpoint
    {
        /// <summary>
        ///     Adds a fieldModel to the server
        /// </summary>
        /// <param name="newField"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void AddField(FieldModel newField, Action<FieldModel> success, Action<Exception> error);

        /// <summary>
        ///     Updates a field model on the server.
        /// </summary>
        /// <param name="fieldToUpdate"></param>
        /// <param name="success"></param>
        /// <param name="error"></param>
        void UpdateField(FieldModel fieldToUpdate, Action<FieldModel> success, Action<Exception> error);

        Task GetField(string id, Func<FieldModel, Task> success, Action<Exception> error);
        Task DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error);
    }
}