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

            var id = UtilShared.GetDeterministicGuid(name);
            var idString = id.ToString().ToUpper();
            key = RESTClient.Instance.Fields.GetController<KeyController>(idString);
            if (key != null)
            {
                _nameDictionary[name] = key;
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
                    string data = KeyModel.Name;
                    UndoCommand newEvent = new UndoCommand(() => Name = value, () => Name = data);

                    KeyModel.Name = value;
                    UpdateOnServer(newEvent);
                    OnFieldModelUpdated(null);
                }
            }
        }

        public KeyModel KeyModel => Model as KeyModel;

        /// <summary>
        /// Use this contructor only if you really need to give this key a specific ID, otherwise use the constructor where you just pass in a name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="guid"></param>
        private KeyController(string name, Guid guid) : base(new KeyModel(name, guid.ToString()))
        {
            HashCode = Id.GetHashCode();
        }

        public KeyController(KeyModel model) : base(model)
        {
            Debug.Assert(!_nameDictionary.ContainsKey(model.Name));
            _nameDictionary[model.Name] = this;

            HashCode = Id.GetHashCode();
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

        private int HashCode { get; } 
        public override int GetHashCode()
        {
            return HashCode;
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

        public override object GetValue()
        {
            return Name;
        }

        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            return matcher.Matches(Name);
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
