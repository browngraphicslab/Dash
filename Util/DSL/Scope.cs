using DashShared;
using System.Collections.Generic;
using System.Linq;

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

        public virtual void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            var value = GetVariable(variableName);
            if (value != null) throw new ScriptExecutionException(new DuplicateVariableDeclarationErrorModel(variableName, value));
            _dictionary[variableName] = valueToSet;

            //add varible to autosuggest option
            DishReplView.NewVariable(variableName);
        }

        public virtual void SetVariable(string variableName, FieldControllerBase valueToSet)
        {
            var child = this;
            while (child != null && !child._dictionary.ContainsKey(variableName)) { child = child.Parent; }
            if (child == null) return;
            child._dictionary[variableName] = valueToSet;
        }

        public FieldControllerBase GetVariable(string variableName)
        {
            var child = this;
            while (child != null && !child._dictionary.ContainsKey(variableName)) { child = child.Parent; }
            return child?._dictionary[variableName];
        }

        public void DeleteVariable(string variableName)
        {
            _dictionary.Remove(variableName);
        }

        public Scope GetFirstAncestor() { return Parent == null ? this : Parent.GetFirstAncestor(); }
        public virtual void SetReturn(FieldControllerBase ret) { Parent.SetReturn(ret); }
        public virtual FieldControllerBase GetReturn => Parent.GetReturn;
    }
}
