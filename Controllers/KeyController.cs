using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dash.Controllers.Operators;
using DashShared;

namespace Dash
{
    //Abstract class from "KeyController<T>" should inherit.
    [DebuggerDisplay("{Name}")]
    public class KeyController : FieldModelController<KeyModel>
    {

        private static Dictionary<string, string> _nameDictionary = new Dictionary<string, string>();

        public string Name
        {
            get => KeyModel.Name;
            set
            {
                if (KeyModel.Name != value)
                {
                    SetName(value);
                }
            }
        }

        /*
       * Sets the data property and gives UpdateOnServer an UndoCommand 
       */
        private void SetName(string val, bool withUndo = true)
        {
            string data = KeyModel.Name;
            UndoCommand newEvent = new UndoCommand(() => SetName(val, false), () => SetName(data, false));

            KeyModel.Name = val;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        public KeyModel KeyModel => Model as KeyModel;
        public KeyController(string name) : this(name, _nameDictionary.TryGetValue(name, out var id) ? id : UtilShared.GetDeterministicGuid(name).ToString())
        {
        }

        private KeyController(string name, string id) : base(new KeyModel(name, id))
        {
            if (!_nameDictionary.ContainsKey(name))
            {
                _nameDictionary[name] = id;
                SaveOnServer();
            }
        }

        /// <summary>
        /// Use this contructor only if you really need to give this key a specific ID, otherwise use the constructor where you just pass in a name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="guid"></param>
        public KeyController(string name, Guid guid) : base(new KeyModel(name, guid.ToString()))
        {
            Debug.Assert(!_nameDictionary.ContainsKey(name) || _nameDictionary[name] == Id);
            if (!_nameDictionary.ContainsKey(name))
            {
                _nameDictionary[name] = Id;
                SaveOnServer();
            }
        }

        public KeyController() : this(Guid.NewGuid().ToString())
        {
        }

        public KeyController(KeyModel model) : base(model)
        {
            var upper = model.Id.ToUpper();
            if (upper == "AOEKMA9J-IP37-96HI-VJ36-IHFI39AHI8DE")
            {
                model = new KeyModel(model.Name, "D154932B-D770-483B-903F-4887038394FD");
            }
            if (upper == "ICON7D27-FA81-4D88-B2FA-42B7888525AF")
            {
                model = new KeyModel(model.Name, "8C8B7C69-8A09-40F3-BEE4-28B64E82CE08");
            }
            UpdateOnServer(null);
            Debug.Assert(!_nameDictionary.ContainsKey(model.Name) || _nameDictionary[model.Name] == model.Id);
            _nameDictionary[model.Name] = model.Id;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            var k = obj as KeyController;
            return k != null && k.Id.Equals(Id);
        }

        public override int GetHashCode()
        {

            return Id.GetHashCode();

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
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return searchString == null || (Name.ToLower().Contains(searchString.ToLower()) ||
               reg.IsMatch(Name)) ? new StringSearchModel(Name) : StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return DSL.GetFuncName<KeyOperator>() + $"(\"{Name}\")";
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
