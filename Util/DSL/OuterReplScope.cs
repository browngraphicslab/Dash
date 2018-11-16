using System.Collections.Generic;

namespace Dash
{
    public class OuterReplScope : Scope
    {
         private readonly DocumentController _variableDoc;

        public DocumentController VariableDoc()
        {
            return _variableDoc;
        }

        public OuterReplScope(DocumentController doc)
        {
            _variableDoc = doc;

            foreach (var var in _variableDoc.EnumFields())
            {
                _dictionary.Add(var.Key.Name, var.Value);
            }
        }

        public OuterReplScope()
        {
            _variableDoc = new DocumentController();
        }

        public override void DeclareVariable(string variableName, FieldControllerBase valueToSet)
        {
            if (TryGetVariable(variableName, out var _)) return;
            _dictionary[variableName] = valueToSet;

            //add varible to autosuggest option
            DishReplView.NewVariable(variableName);

            var key = KeyController.Get(variableName);
            _variableDoc.SetField(key, valueToSet, true);
        }

        public override void SetVariable(string variableName, FieldControllerBase valueToSet)
        {
            base.SetVariable(variableName, valueToSet);

            var key = KeyController.Get(variableName);
            _variableDoc.SetField(key, valueToSet, true);
        }
    }
}
