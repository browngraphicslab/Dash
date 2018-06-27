using System.Collections.Generic;
using System.Linq;
using DashShared;

namespace Dash
{
    internal class ArrayExpression : ScriptExpression
    {
        private readonly List<ScriptExpression> list;

        public ArrayExpression(List<ScriptExpression> list) => this.list = list;

        public override TypeInfo Type => list.Last().Type;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new System.NotImplementedException();
        }

        public override FieldControllerBase Execute(Scope scope)
        {
             var typeInfo = TypeInfo.None;
            //  execute each element in list if it isn't null
            var outputList = new List<FieldControllerBase>();
            foreach (var elem in list)
            {
                if (elem != null)
                {
                    var field = elem.Execute(scope);
                    outputList.Add(field);

                    if (typeInfo == TypeInfo.None && field.TypeInfo != TypeInfo.None)
                    {
                        typeInfo = field.TypeInfo;
                    } else if(typeInfo != field.TypeInfo)
                    {
                        typeInfo = TypeInfo.Any;
                    }
                }
            }

            var lc = (BaseListController)FieldControllerFactory.CreateDefaultFieldController(TypeInfo.List, typeInfo);
            foreach (var item in outputList)
            {
                lc.AddBase(item);
            }
            //lc.AddRange(outputList);

            return lc;
        }
    }
}