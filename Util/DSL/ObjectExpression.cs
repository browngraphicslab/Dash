using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    class ObjectExpression : ScriptExpression
    {
        private readonly List<Node> dictionary;
        //private readonly ScriptExpression value;

        public ObjectExpression(List<Node> dic/*, ScriptExpression val*/)
        {
            this.dictionary = dic;
            //this.value = val;
        }

        public override TypeInfo Type => TypeInfo.Any;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new System.NotImplementedException();
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            DocumentController result = new DocumentController();
            foreach (var property in dictionary)
            {
                var propChildren = property.Children;
                Debug.Assert(propChildren[0] is Identifier);
                var keyString = ((Identifier)propChildren[0]).Text;
                var key = new KeyController(keyString, keyString);
                var value = TypescriptToOperatorParser.ParseToExpression(propChildren[1]).Execute(scope);
                result.SetField(key, value, true);
            }

            return result;
        }
    }
}
