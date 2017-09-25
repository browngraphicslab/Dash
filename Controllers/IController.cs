using System.Diagnostics;
using DashShared;

namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public abstract class IController<T> : IControllerBase where T:EntityBase
    {
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

        public void UpdateOnServer()
        {
            
        }

        public void DeleteOnServer()
        {
            
        }

        public void SaveOnServer()
        {
            
        }

    }
}
