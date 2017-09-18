using DashShared;

namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public interface IController<T> where T:EntityBase
    {
        
        /// <summary>
        /// the model that this controller controls
        /// </summary>
        T Model { get; }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        string GetId();

    }
}
