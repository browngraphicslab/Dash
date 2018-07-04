﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static string GetId(string name)
        {
            if (_nameDictionary.ContainsKey(name))
            {
                return _nameDictionary[name];
            }

            var id = Guid.NewGuid().ToString();
            _nameDictionary[name] = id;
            return id;
        }

        public KeyModel KeyModel => Model as KeyModel;
        public KeyController(string name) : base(new KeyModel(name, GetId(name)))
        {
            IsOnServer(delegate (bool onServer)
            {
                if (!onServer)
                {
                    SaveOnServer();
                }
            });
        }

        public KeyController(string name, string guid) : base(new KeyModel(name, guid))
        {
            IsOnServer(delegate (bool onServer)
            {
                if (!onServer)
                {
                    SaveOnServer();
                }
            });
            Debug.Assert(!_nameDictionary.ContainsKey(name));
            _nameDictionary[name] = guid;
        }

        public KeyController() : this(Guid.NewGuid().ToString())
        {
        }

        public KeyController(KeyModel model) : base(model)
        {
            Debug.Assert(!_nameDictionary.ContainsKey(model.Name));
            _nameDictionary[model.Name] = model.Id;
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
            var reg = new System.Text.RegularExpressions.Regex(searchString);
            return searchString == null || (Name.ToLower().Contains(searchString.ToLower()) ||
               reg.IsMatch(Name)) ? new StringSearchModel(Name) : StringSearchModel.False;
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
