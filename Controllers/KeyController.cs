using System.Runtime.InteropServices.WindowsRuntime;
using DashShared;

namespace Dash
{
    public class KeyController : ViewModelBase, IController<KeyModel>
    {
        /// <summary>
        ///     The fieldModel associated with this <see cref="FieldModelController"/>, You should only set values on the controller, never directly
        ///     on the fieldModel!
        /// </summary>
        public KeyModel Model { get; set; }

        private readonly int _hash;

        public string Name
        {
            get => Model.Name;
            set
            {
                Model.Name = value;
                RESTClient.Instance.Keys.UpdateKey(Model, model =>
                {
                    // Yay!
                }, exception =>
                {
                    // Hayyyyy!
                });
            }
        }

        public string Id => Model.Id;

        public KeyController(KeyModel keyModel, bool sendToServer = true)
        {
            // Initialize Local Variables
            Model = keyModel;
            _hash = Model.GetHashCode();
            ContentController<KeyModel>.AddController(this);

            if (sendToServer)
            {
                RESTClient.Instance.Keys.AddKey(Model, model =>
                {
                    // Yay!
                }, exception =>
                {
                    // Hayyyyy!
                });
            }
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
            var k = obj as KeyController;
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
