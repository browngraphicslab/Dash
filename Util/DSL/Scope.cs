using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Flurl.Util;

namespace Dash
{
    public class Scope : IEnumerable<KeyValuePair<string, FieldControllerBase>>
    {
        public static string THIS_NAME = "this";

        protected readonly Dictionary<string, FieldControllerBase> _dictionary;
        public Scope Parent;

        public Scope(IDictionary<string, FieldControllerBase> existingScope = null) { _dictionary = existingScope != null ? new Dictionary<string, FieldControllerBase>(existingScope) : new Dictionary<string, FieldControllerBase>(); }

        public Scope(Scope parentScope) { Parent = parentScope; _dictionary = new Dictionary<string, FieldControllerBase>(); }

        public static Scope CreateStateWithThisDocument(DocumentController thisDocument)
        {
            var scope = new Scope();
            scope.DeclareVariable(THIS_NAME, thisDocument);
            return scope;
        }

        public DocumentController ToDocument(bool useParent)
        {
            var doc = new DocumentController();
            AddToDocument(doc, useParent);
            return doc;
        }

        private void AddToDocument(DocumentController doc, bool useParent)
        {
            if (useParent)
            {
                Parent?.AddToDocument(doc, true);
            }

            foreach (var fieldControllerBase in _dictionary)
            {
                doc.SetField(KeyController.Get(fieldControllerBase.Key), fieldControllerBase.Value, true);
            }
        }

        public static Scope FromDocument(DocumentController doc)
        {
            var scope = new Scope();
            foreach (var field in doc.EnumDisplayableFields())
            {
                scope.SetVariable(field.Key.Name, field.Value);
            }

            return scope;
        }

        public FieldControllerBase this[string variableName]
        {
            get => GetVariable(variableName);
            set => SetVariable(variableName, value);
        }

        public virtual void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            if (_dictionary.TryGetValue(variableName, out var value)) throw new ScriptExecutionException(new DuplicateVariableDeclarationErrorModel(variableName, value));
            _dictionary[variableName] = valueToSet;

            //add varible to autosuggest option
            //TODO Add this variable a different way, possibly with an event, or by looking though the scope in the repl
            DishReplView.NewVariable(variableName);  // bcz: causes a crash in the KeyValue Pane // tfs: this should be fixed, but this call should go away anyway
        }

        public virtual void SetVariable(string variableName, FieldControllerBase valueToSet)
        {
            Scope child = this;
            while (child != null && !child._dictionary.ContainsKey(variableName) && child.Parent != null) { child = child.Parent; }
            child._dictionary[variableName] = valueToSet;
        }

        public FieldControllerBase GetVariable(string variableName)
        {
            return TryGetVariable(variableName, out var field) ? field : null;
        }

        public bool TryGetVariable(string variableName, out FieldControllerBase value)
        {
            Scope child = this;
            while (child != null && !child._dictionary.ContainsKey(variableName)) { child = child.Parent; }

            if (child == null)
            {
                value = null;
                return false;
            }

            value = child._dictionary[variableName];
            return true;

        }

        public bool CanDeclareVariable(string variableName)
        {
            return !_dictionary.ContainsKey(variableName);
        }

        public void DeleteVariable(string variableName)
        {
            _dictionary.Remove(variableName);
        }

        public Scope Merge(Scope scope)
        {
            var outScope = new Scope(this);

            foreach (var kv in scope.CollectVariables()) { outScope._dictionary.Add(kv.Key, kv.Value); }

            return outScope;
        }

        public Dictionary<string, FieldControllerBase> CollectVariables()
        {
            var collector = new Dictionary<string, FieldControllerBase>();

            var child = this;
            while (child != null)
            {
                var items = child._dictionary.ToKeyValuePairs().ToList();
                items.ForEach(kv => collector.Add(kv.Key, (FieldControllerBase) kv.Value));
                child = child.Parent;
            }

            return collector;
        }

        public IEnumerator<KeyValuePair<string, FieldControllerBase>> GetEnumerator()
        {
            foreach (var kvp in _dictionary)
            {
                yield return kvp;
            }

            if (Parent != null)
            {
                foreach (var kvp in Parent)
                {
                    yield return kvp;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
