using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    //Abstract class from "KeyController<T>" should inherit.
    public class KeyController : IController<KeyModel>
    {

        public string Name
        {
            get => Model.Name;
            set
            {
                Model.Name = value;
                UpdateOnServer();
            }
        }
        public KeyController(string guid, bool saveOnServer = true) : this(new KeyModel(guid))
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
        }

        public KeyController(string guid, string name, bool saveOnServer = true) : this(new KeyModel(guid, name))
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
        }

        public KeyController(bool saveOnServer = true) : this(new KeyModel())
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
        }

        public KeyController(KeyModel model, bool saveOnServer = true) : base(model)
        {
            if (saveOnServer)
            {
                SaveOnServer();
            }
        }

        public override string ToString()
        {
            return this.Name;
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

        public bool IsUnrenderedKey()
        {
            return Equals(KeyStore.DelegatesKey) ||
                   Equals(KeyStore.PrototypeKey) ||
                   Equals(KeyStore.LayoutListKey) ||
                   Equals(KeyStore.ActiveLayoutKey) ||
                   Equals(KeyStore.IconTypeFieldKey);
        }

        public virtual void Dispose()
        {

        }
    }
}
