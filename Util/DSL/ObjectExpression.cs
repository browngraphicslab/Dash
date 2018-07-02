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
        private readonly Dictionary<string, FieldControllerBase> dictionary;

        public ObjectExpression(Dictionary<string, FieldControllerBase> dic) => this.dictionary = dic;

        public override TypeInfo Type => TypeInfo.Any;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new System.NotImplementedException();
        }

        public override FieldControllerBase Execute(Scope scope)
        {

            //DocumentController result = new DocumentController();
            //foreach (var property in dictionary)
            //{
            //    var propChildren = property.Children;
            //    Debug.Assert(propChildren[0] is Identifier);
            //    var keyString = ((Identifier)propChildren[0]).Text;
            //    var key = new KeyController(keyString, keyString);
            //    var value = (ParseToExpression(propChildren[1]) as LiteralExpression)?.GetField();
            //    result.SetField(key, value, true);
            //}
            return null;
        }
    }
}
