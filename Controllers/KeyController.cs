using System.Runtime.InteropServices.WindowsRuntime;
using DashShared;

namespace Dash
{
    public class KeyController : ViewModelBase, IController
    {
        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController"/>, You should only set values on the controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public KeyModel KeyModel { get; set; }

        private readonly int _hash;

        public string Name
        {
            get { return KeyModel.Name; }
            set { KeyModel.Name = value; }
        }

        public string Id => KeyModel.Id;

        public KeyController(KeyModel keyModel)
        {
            // Initialize Local Variables
            KeyModel = keyModel;
            _hash = KeyModel.GetHashCode();
            ContentController.AddModel(keyModel);
            ContentController.AddController(this);
        }

        public KeyController(string guid) : this(new KeyModel(guid))
        {
        }

        public KeyController(string guid, string name) : this(new KeyModel(guid, name))
        {
        }

        public KeyController() : this(new KeyModel())
        {
        }

        /// <summary>
        /// Returns the <see cref="EntityBase.Id"/> for the entity which the controller encapsulates
        /// </summary>
        public string GetId()
        {
            return KeyModel.Id;
        }

        /// <summary>
        /// Gets the name of the key.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return KeyModel.Name;
        }

        public override bool Equals(object obj)
        {
            var k = obj as KeyController;
            return k != null && k.Id.Equals(GetId());
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return KeyModel.Name;
        }

        public bool IsUnrenderedKey()
        {
            return KeyModel.Name.StartsWith("_");
            //return Equals(KeyStore.DelegatesKey) ||
            //       Equals(KeyStore.PrototypeKey) ||
            //       Equals(KeyStore.LayoutListKey) ||
            //       Equals(KeyStore.ActiveLayoutKey) ||
            //       Equals(KeyStore.IconTypeFieldKey);
        }

        public virtual void Dispose()
        {
        }
    }
}
