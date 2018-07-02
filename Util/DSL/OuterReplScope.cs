using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class OuterReplScope : Scope
    {
        //private readonly Dictionary<string, FieldControllerBase> _dictionary;

        private DocumentController variableDoc;

        public OuterReplScope(DocumentController doc)
        {
            variableDoc = doc.GetField<DocumentController>(KeyStore.ReplScopeKey);

            _dictionary = new Dictionary<string, FieldControllerBase>();
            foreach (var var in variableDoc.EnumFields())
            {
                _dictionary.Add(var.Key.Name, var.Value);
            }
        }
        public override void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            if (GetVariable(variableName) != null) return;
            _dictionary[variableName] = valueToSet;

            //add varible to autosuggest option
            DishReplView.NewVariable(variableName);

            var key = new KeyController(variableName, variableName);
            variableDoc.SetField(key, valueToSet, true);
        }

        public override void SetVariable(string variableName, FieldControllerBase valueToSet)
        {
            var child = (Scope)this;
            while (child != null && !this._dictionary.ContainsKey(variableName)) { child = child.Parent; }
            if (child == null) return;
            this._dictionary[variableName] = valueToSet;

            var key = new KeyController(variableName, variableName);
            variableDoc.SetField(key, valueToSet, true);
        }
    }
}
