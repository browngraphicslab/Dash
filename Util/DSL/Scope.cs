using DashShared;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class Scope
    {
        public static string THIS_NAME = "this";

        private FieldControllerBase _returnValue;

        private readonly Dictionary<string, FieldControllerBase> _dictionary;
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

        public void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            //TODO: Throw exception or provide feedback if attempted duplicate declaration
            if (GetVariable(variableName) != null) return;
            _dictionary[variableName] = valueToSet;
        }

        public void SetVariable(string variableName, FieldControllerBase valueToSet)
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

        public Scope GetFirstAncestor() { return Parent == null ? this : Parent.GetFirstAncestor(); }
        public void SetReturn(FieldControllerBase ret) { _returnValue = ret; }
        public FieldControllerBase GetReturn => _returnValue;
    }
}
