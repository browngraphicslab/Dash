using System.Runtime.InteropServices.WindowsRuntime;
using DashShared;

namespace Dash
{
    public abstract class KeyControllerGeneric<T> : KeyController where T: KeyModel
    {
        private readonly int _hash;

        public KeyControllerGeneric(KeyModel keyModel) : base(keyModel)
        {
            _hash = Model.GetHashCode();
        }

        public virtual void Dispose()
        {
        }
    }
}
