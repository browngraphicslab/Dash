using System;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    //Abstract class from "KeyController<T>" should inherit.
    [DebuggerDisplay( "{Name}")]
    public class KeyController : FieldModelController<KeyModel>
    {

        public string Name
        {
            get => KeyModel.Name;
            set
            {
                KeyModel.Name = value;
                OnFieldModelUpdated(null);
            }
        }
        
        public KeyModel KeyModel => Model as KeyModel;
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

        public override void Init()
        {

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

        static public KeyController LookupKeyByName(string name, bool createIfNull = false)
        {
            foreach (var k in ContentController<FieldModel>.GetControllers<KeyController>())
            {
                if (k.Name == name)
                    return k;
            }
            return createIfNull ?
                new KeyController(DashShared.UtilShared.GenerateNewId(), name) : null;
        }

        public override FieldControllerBase Copy()
        {
            return this;
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

        public override TypeInfo TypeInfo { get; }
        public override bool TrySetValue(object value)
        {
            var name = value as string;
            if (name != null)
            {
                Name = name;
                return true;
            }
            return false;
        }

        public override object GetValue(Context context)
        {
            return Name;
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return Name.ToLower().Contains(searchString) ? new StringSearchModel(Name) : StringSearchModel.False;
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }
        /*

        public static Dictionary<staticKey, Tuple<string, string>> _dict = new Dictionary<staticKey, Tuple<string, string>>()
        {
            {staticKey.Layout, new Tuple<string, string>("collection","id")}
        };

        public static KeyController Get(staticKey key)
        {
            return ContentController<KeyModel>.GetController<KeyController>(_dict[key]);
        }

        public enum staticKey
        {
            Layout
        }*/
    }
}
