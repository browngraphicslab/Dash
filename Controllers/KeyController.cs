using System.Runtime.InteropServices.WindowsRuntime;
using DashShared;

namespace Dash
{
    public abstract class KeyController<T> : KeyControllerBase where T: KeyModel
    {
        private readonly int _hash;

        public KeyController(KeyModel keyModel) : base(keyModel)
        {
            _hash = Model.GetHashCode();
        }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return Model.Id;
        }

        /// <summary>
        /// Gets the name of the key.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return Model.Name;
        }

        public override bool Equals(object obj)
        {
            var k = obj as KeyController<T>;
            return k != null && k.Id.Equals(GetId());
        }

        public override int GetHashCode()
        {
            return GetId().GetHashCode();
        }

        public override string ToString()
        {
            return Model.Name;
        }

        public virtual void Dispose()
        {
        }
    }
}
