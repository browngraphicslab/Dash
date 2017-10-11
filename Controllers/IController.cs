using System;
using System.Diagnostics;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public abstract class IController<T> : IControllerBase where T:EntityBase
    {
        private static IModelEndpoint<T> _serverEndpoint= RESTClient.Instance.GetEndpoint<T>();
        public IController(T model)
        {
            Debug.Assert(model != null);
            Model = model;
            ContentController<T>.AddController(this);
        }
        
        /// <summary>
        /// the model that this controller controls
        /// </summary>
        public T Model { get; private set; }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return Model.Id;
        }

        public string Id => Model.Id;

        public virtual void UpdateOnServer(Action<T> success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.UpdateDocument(Model, success, error);
        }

        public virtual void DeleteOnServer(Action success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.DeleteDocument(Model, success, error);
        }

        /// <summary>
        /// This should only be called the first time you make the model, otherwise use "UpdateOnServer" to save;
        /// </summary>
        public virtual void SaveOnServer(Action<T> success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.AddDocument(Model, success, error);
        }

    }
}
