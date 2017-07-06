using DashShared;


namespace Dash
{
    /// <summary>
    /// The Base class for all controllers which communicate with the server
    /// </summary>
    public interface IController
    {

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        string GetId();

    }
}
