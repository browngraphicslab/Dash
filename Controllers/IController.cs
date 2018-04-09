using System;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public abstract class IController<T> : IControllerBase where T:EntityBase
    {   
        // == CONSTRUCTOR ==
        public IController(T model)
        {
            Debug.Assert(model != null);
            Model = model;
            ContentController<T>.AddController(this);
        }

        // == FIELDS ==
        // fetches the endoiint for server interations
        private static IModelEndpoint<T> _serverEndpoint = RESTClient.Instance.GetEndpoint<T>();

        /// <summary>
        /// The model that this controller controls. Can only be changed internally.
        /// </summary>
        public T Model { get; private set; }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return Model.Id;
        }

        public string Id => Model.Id; // TODO: this is the same as above, conslidate

        // == METHODS ==
        /// <summary>
        /// Method which should store all the initlization methods for the controller. 
        /// Anything that gets other controllers' references should be put in here instead of the constructor
        /// </summary>
        public abstract void Init();/// <summary>

        /// <summary>
        /// Overrides default controller behavior with '==' operator to use underlying .Equals method.
        /// Note: generally, you should just use .Equals() for code clarity and to guarantee behavior.
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
            return (c1.Equals(c2));
        }

        /// <summary>
        /// Overrides default behavior for '!=' (not equals) operator s.t. it
        /// behaves via the underlying Equals method.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator !=(IController<T> c1, IController<T> c2)
        {
            return c1 == null ? !(c2 == null) : !(c1.Equals(c2));
        }

        /// <summary>
        /// Pushes local changes in the controller's underlying model to the server.
        /// </summary>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public virtual void UpdateOnServer(Action<T> success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.UpdateDocument(Model, success, error);
        }

        /// <summary>
        /// Deletes the given controller's underlying model from the server.
        /// </summary>
        /// <param name="success"></param>
        /// <param name="error"></param>
        public virtual void DeleteOnServer(Action success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.DeleteDocument(Model, success, error);
        }

        /// <summary>
        /// Saves the given controllers' underlying model on the server.
        /// This should only be called the first time you make the model, otherwise use "UpdateOnServer" to save;
        /// </summary>
        public virtual void SaveOnServer(Action<T> success = null, Action<Exception> error = null)
        {
            error = error ?? ((e) => throw e);
            _serverEndpoint.AddDocument(Model, success, error);
        }

    }
}
