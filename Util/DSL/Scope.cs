using System.Collections.Generic;
using System.Linq;
using Flurl.Util;

namespace Dash
{
    public class Scope
    {
        public static string THIS_NAME = "this";

        private FieldControllerBase _returnValue;

        internal Dictionary<string, FieldControllerBase> _dictionary;
        public Scope Parent;

        public Scope(IDictionary<string, FieldControllerBase> existingScope = null) { _dictionary = existingScope != null ? new Dictionary<string, FieldControllerBase>(existingScope) : new Dictionary<string, FieldControllerBase>(); }

        public Scope(Scope parentScope) { Parent = parentScope; _dictionary = new Dictionary<string, FieldControllerBase>(); }

        public static Scope CreateStateWithThisDocument(DocumentController thisDocument)
        {
            var scope = new Scope();
            scope.DeclareVariable(THIS_NAME, thisDocument);
            return scope;
        }

        public FieldControllerBase this[string variableName]
        {
            get => GetVariable(variableName);
            set => SetVariable(variableName, value);
        }

        public virtual void DeclareVariable(string variableName, FieldControllerBase targetValue)
        {
            var existingValue = GetVariable(variableName);
            if (existingValue != null)
            {
                bool alreadyEqual = targetValue.GetValue(null).Equals(existingValue.GetValue(null));
                throw new ScriptExecutionException(new DuplicateVariableDeclarationErrorModel(variableName, targetValue, existingValue, alreadyEqual));
            }
            _dictionary[variableName] = targetValue;

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
            Scope child = this;
            while (child != null && !child._dictionary.ContainsKey(variableName)) { child = child.Parent; }
            return child?._dictionary[variableName];
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

        public Scope GetFirstAncestor() { return Parent == null ? this : Parent.GetFirstAncestor(); }
        public virtual void SetReturn(FieldControllerBase ret) { Parent.SetReturn(ret); }
        public virtual FieldControllerBase GetReturn => Parent.GetReturn;
    }
}
