using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public abstract class Controller<T> where T:EntityBase
    {   
        // == CONSTRUCTOR ==
        protected Controller(T model)
        {
            Debug.Assert(model != null);
            Model = model;
        }

        // == FIELDS ==
        // fetches the endoiint for server interations
        private static IModelEndpoint<T> _serverEndpoint = RESTClient.Instance.GetEndpoint<T>();

        /// <summary>
        /// The model that this controller controls. Can only be changed internally.
        /// </summary>
        public T Model { get; private set; }

        public string Id => Model.Id;

        /// <summary>
        /// Overrides default controller behavior with '==' operator to use underlying .Equals method.
        /// Note: generally, you should just use .Equals() for code clarity and to guarantee behavior.
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator ==(Controller<T> c1, Controller<T> c2)
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
        public static bool operator !=(Controller<T> c1, Controller<T> c2)
        {
            return c1 == null ? !(c2 == null) : !(c1.Equals(c2));
        }

        /// <summary>
        /// Pushes local changes in the controller's underlying model to the server.
        /// </summary>
        /// <param name="undoEvent"></param>
        public void UpdateOnServer(UndoCommand undoEvent)
        {
            if(undoEvent != null)
            {
                UndoManager.EventOccured(undoEvent);
            }

            _serverEndpoint.UpdateDocument(this);
        }

        /// <summary>
        /// Deletes the given controller's underlying model from the server.
        /// </summary>
        public void DeleteOnServer()
        {
            _serverEndpoint.DeleteDocument(Model);
        }

        /// <summary>
        /// Saves the given controllers' underlying model on the server.
        /// This should only be called the first time you make the model, otherwise use "UpdateOnServer" to save;
        /// </summary>
        public void SaveOnServer()
        {
            _serverEndpoint.AddDocument(this);
        }

        public Task<bool> IsOnServer()
        {
            return _serverEndpoint.HasDocument(Model);
        }
    }
}
