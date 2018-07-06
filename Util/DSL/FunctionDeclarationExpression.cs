﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    class FunctionDeclarationExpression : ScriptExpression
    {
        private readonly string _funcName;
        private readonly NodeArray<ParameterDeclaration> _parameters;
        private readonly ScriptExpression _funcBlock;
        private readonly DashShared.TypeInfo _returnType;


        public FunctionDeclarationExpression(string name, NodeArray<ParameterDeclaration> param, 
            ScriptExpression fB, DashShared.TypeInfo retur)
        {
            _funcName = name;
            _parameters = param;
            _funcBlock = fB;
            _returnType = retur;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            //var boolKey = ForOperatorController.BoolKey;
            //var blockKey = ForOperatorController.ForBlockKey;
            //var incrementKey = ForOperatorController.IncrementKey;
            //var countDecKey = ForOperatorController.CounterDeclarationKey;

            //var inputs = new Dictionary<KeyController, FieldControllerBase>
            //{
            //    { countDecKey, _parameters[countDecKey].Execute(scope) },
            //    { boolKey, _parameters[boolKey].Execute(scope) }
            //};

            var functionOperator = new FunctionOperatorController(_parameters, _funcBlock, _returnType);


            scope?.DeclareVariable(_funcName, functionOperator);

            return new TextController("");
        }

  

        public override FieldControllerBase CreateReference(Scope scope)
        {
            //return OperatorScript.CreateDocumentForOperator(
            //    _parameters.Select(
            //        kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
            //            kvp.Value.CreateReference(scope))), _opName); //recursive linq

            return null;
        }

        public override DashShared.TypeInfo Type => _returnType;
    }
}