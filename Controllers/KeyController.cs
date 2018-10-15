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

        private static Dictionary<string, KeyController> _nameDictionary = new Dictionary<string, KeyController>();

        public static KeyController Get(string name)
        {
            if (_nameDictionary.TryGetValue(name, out var key))
            {
                return key;
            }

            key = new KeyController(name, Guid.NewGuid());
            _nameDictionary[name] = key;
            return key;
        }

        public static KeyController Get(string name, Guid id)
        {
            if (_nameDictionary.TryGetValue(name, out var key))
            {
                Debug.Assert(id.ToString().ToUpper() == key.Id);
                return key;
            }

            key = new KeyController(name, id);
            _nameDictionary[name] = key;
            return key;
        }

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

        private static string _hackId;
        public KeyModel KeyModel => Model as KeyModel;

        /// <summary>
        /// Use this contructor only if you really need to give this key a specific ID, otherwise use the constructor where you just pass in a name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="guid"></param>
        private KeyController(string name, Guid guid) : base(new KeyModel(name, guid.ToString()))
        {
        }

        public KeyController(KeyModel model) : base(model)
        {
            Debug.Assert(!_nameDictionary.ContainsKey(model.Name));
            _nameDictionary[model.Name] = this;
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
