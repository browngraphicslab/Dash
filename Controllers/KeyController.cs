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
            ContentController.AddModel(keyModel);
            ContentController.AddController(this);

            RESTClient.Instance.Keys.AddKey(KeyModel, model =>
            {
                // Yay!
            }, exception =>
            {
                // Hayyyyy!
            });
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
            return GetId().GetHashCode();
        }

        public override string ToString()
        {
            return KeyModel.Name;
        }

        public virtual void Dispose()
        {
        }
    }
}
