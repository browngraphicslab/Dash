using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Flurl.Util;

namespace Dash
{
    public abstract class Scope : IEnumerable<KeyValuePair<string, FieldControllerBase>>
    {
        public static string THIS_NAME = "this";

        protected Scope(Scope parent, bool defaultToGlobal = true)
        {
            Parent = parent ?? (defaultToGlobal ? DocumentScope.GetGlobalScope() : null);
        }

        public void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            var value = GetLocalVariable(variableName);
            if(value != null) throw new ScriptExecutionException(new DuplicateVariableDeclarationErrorModel(variableName, value));
            SetLocalVariable(variableName, valueToSet);

            //add varible to autosuggest option
            //TODO Add this variable a different way, possibly with an event, or by looking though the scope in the repl
            DishReplView.NewVariable(variableName);  // bcz: causes a crash in the KeyValue Pane // tfs: this should be fixed, but this call should go away anyway
        }

        public void SetVariable(string variableName, FieldControllerBase value)
        {
            Scope child = this;
            while (!child.TryGetVariable(variableName, out var _) && child.Parent != null) { child = child.Parent; }

            child.SetLocalVariable(variableName, value);
        }

        public FieldControllerBase GetVariable(string variableName)
        {
            return TryGetVariable(variableName, out var field) ? field : null;
        }


        public bool TryGetVariable(string variableName, out FieldControllerBase value)
        {
            Scope child = this;
            value = null;
            while (child != null && (value = child.GetLocalVariable(variableName)) == null)
            {
                child = child.Parent;
            }

            return value != null;
        }

        public bool HasVariable(string variableName)
        {
            var scope = this;
            while (scope != null)
            {
                if (scope.HasLocalVariable(variableName))
                {
                    return true;
                }

                scope = scope.Parent;
            }

            return false;
        }

        public void DeleteVariable(string variableName)
        {
            Scope child = this;
            while (child != null && !child.TryGetVariable(variableName, out var _)) { child = child.Parent; }

            child?.DeleteLocalVariable(variableName);
        }

        public abstract IEnumerator<KeyValuePair<string, FieldControllerBase>> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Scope CreateStateWithThisDocument(DocumentController thisDocument)
        {
            var scope = new DictionaryScope();
            scope.DeclareVariable(THIS_NAME, thisDocument);
            return scope;
        }

        public DocumentController ToDocument(bool useParent)
        {
            var doc = new DocumentController();
            AddToDocument(doc, useParent);
            return doc;
        }

        protected void AddToDocument(DocumentController doc, bool useParent)
        {
            if (useParent)
            {
                Parent?.AddToDocument(doc, true);
            }

            foreach (var fieldControllerBase in this)
            {
                doc.SetField(KeyController.Get(fieldControllerBase.Key), fieldControllerBase.Value, true);
            }
        }

        public static Scope FromDocument(DocumentController doc)
        {
            var scope = new DictionaryScope();
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

        protected abstract void SetLocalVariable(string name, FieldControllerBase field);
        protected abstract FieldControllerBase GetLocalVariable(string name);
        protected abstract bool HasLocalVariable(string name);
        protected abstract void DeleteLocalVariable(string variableName);

        public Scope Parent { get; protected set; }

        public Scope Merge(Scope scope)
        {
            var outScope = new DictionaryScope(this);

            foreach (var kv in scope.CollectVariables()) { outScope.SetLocalVariable(kv.Key, kv.Value); }

            return outScope;
        }

        protected Dictionary<string, FieldControllerBase> CollectVariables()
        {
            var collector = new Dictionary<string, FieldControllerBase>();

            Scope child = this;
            while (child != null)
            {
                child.ToList().ForEach(kv => collector.Add(kv.Key, kv.Value));
                child = child.Parent;
            }

            return collector;
        }
    }

    public class DictionaryScope : Scope
    {
        protected readonly Dictionary<string, FieldControllerBase> _dictionary;

        public DictionaryScope(IDictionary<string, FieldControllerBase> existingScope = null) : base(null)
        {
            _dictionary = existingScope != null ? new Dictionary<string, FieldControllerBase>(existingScope) : new Dictionary<string, FieldControllerBase>();
        }

        public DictionaryScope(Scope parentScope) : base(parentScope)
        {
            _dictionary = new Dictionary<string, FieldControllerBase>();
        }

        protected override void SetLocalVariable(string variableName, FieldControllerBase value)
        {
            _dictionary[variableName] = value;
        }

        protected override FieldControllerBase GetLocalVariable(string variableName)
        {
            return _dictionary.TryGetValue(variableName, out var field) ? field : null;
        }

        protected override bool HasLocalVariable(string variableName)
        {
            return _dictionary.ContainsKey(variableName);
        }

        protected override void DeleteLocalVariable(string variableName)
        {
            _dictionary.Remove(variableName);
        }

        public override IEnumerator<KeyValuePair<string, FieldControllerBase>> GetEnumerator()
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
    }
}
