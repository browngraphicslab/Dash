using System;
using System.Diagnostics;
using DashShared;
using DashShared.Models;
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
        /// Only assume that Controllers have been put in the content controller, not that CreateReferences has been called on them
        /// </summary>
        //public abstract void CreateReferences();

        /// <summary>
        /// Method which should store all the initlization methods for the controller. 
        /// Anything that gets other controllers' references should be put in here instead of the constructor
        /// </summary>
        public abstract void Init();/// <summary>

        /// If you get an exception here, you are trying to compare 2 controllers with ==.
        /// This causes problems with data persistence so you should always use .Equals to compare controllers
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator ==(IController<T> c1, IController<T> c2)
        {
            if (ReferenceEquals(c1, null))
            {
                return ReferenceEquals(c2, null);
            }
            if (ReferenceEquals(c2, null))
            {
                return false;
            }
            throw new NotImplementedException();
        }

        public static bool operator !=(IController<T> c1, IController<T> c2)
        {
            return c1 == null ? !(c2 == null) : !(c1.Equals(c2));
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
