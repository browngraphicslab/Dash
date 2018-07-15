using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class OuterReplScope : ReturnScope
    {
         private readonly DocumentController _variableDoc;

        public DocumentController VariableDoc()
        {
            return _variableDoc;
        }

        public OuterReplScope(DocumentController doc) : base(null)
        {
            _variableDoc = doc;

            _dictionary = new Dictionary<string, FieldControllerBase>();
            foreach (var var in _variableDoc.EnumFields())
            {
                _dictionary.Add(var.Key.Name, var.Value);
            }
        }

        public OuterReplScope() : base(null)
        {
            _variableDoc = new DocumentController();

            _dictionary = new Dictionary<string, FieldControllerBase>();
        }

        public override void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            if (GetVariable(variableName) != null) return;
            _dictionary[variableName] = valueToSet;

            //add varible to autosuggest option
            DishReplView.NewVariable(variableName);

            var key = new KeyController(variableName);
            _variableDoc.SetField(key, valueToSet, true);
        }

        public override void SetVariable(string variableName, FieldControllerBase valueToSet)
        {
            var child = (Scope)this;
            while (child != null && !this._dictionary.ContainsKey(variableName)) { child = child.Parent; }
            if (child == null) return;
            this._dictionary[variableName] = valueToSet;

            var key = new KeyController(variableName, variableName);
            _variableDoc.SetField(key, valueToSet, true);
        }
    }
}
