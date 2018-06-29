using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DashShared;

namespace Dash
{
    public enum SelfRefAssignment
    {
        None,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Modulo,
        StringSearch
    }

    public class SelfRefAssignmentExpression : ScriptExpression
    {
        private readonly VariableExpression _var;
        private readonly ScriptExpression _assignExp;
        private readonly Op.Name _opName;

        public SelfRefAssignmentExpression(VariableExpression var, ScriptExpression assignExp, Op.Name opName)
        {
            _var = var;
            _assignExp = assignExp;
            _opName = opName;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var varCtrl = _var.Execute(scope);
            var assignCtrl = _assignExp.Execute(scope);

            var inputs = new List<FieldControllerBase>
            {
                varCtrl,
                assignCtrl
            };

            //if (varCtrl.CheckTypeEquality(assignCtrl))
            //{
            //    switch (varCtrl.TypeInfo)
            //    {
            //        case TypeInfo.Number:
            //            var varNum = ((NumberController) varCtrl).Data;
            //            var assignNum = ((NumberController) assignCtrl).Data;
            //            double outNum = 0;

            //            switch (_selfRefOp)
            //            {
            //                case SelfRefAssignment.Addition:
            //                    _opName = DSL.GetFuncName<AddOperatorController>();
            //                    break;
            //                case SelfRefAssignment.Subtraction:
            //                    _opName = DSL.GetFuncName<SubtractOperatorController>();
            //                    break;
            //                case SelfRefAssignment.Multiplication:
            //                    _opName = DSL.GetFuncName<MultiplyOperatorController>();
            //                    break;
            //                case SelfRefAssignment.Division:
            //                    _opName = DSL.GetFuncName<AddOperatorController>();
            //                    break;
            //                case SelfRefAssignment.Modulo:
            //                    outNum = varNum % assignNum;
            //                    break;
            //            }

            //            scope.SetVariable(_varName, new NumberController(outNum));
            //            break;
            //        case TypeInfo.Text:
            //            var varText = ((TextController) varCtrl).Data;
            //            var assignText = ((TextController) assignCtrl).Data;
            //            var outString = "";

            //            switch (_selfRefOp)
            //            {
            //                case SelfRefAssignment.Addition:
            //                    outString = varText + assignText; break;
            //                case SelfRefAssignment.Division:

            //                case SelfRefAssignment.StringSearch:
            //                    varEditor = new StringBuilder(varText);
            //                    var toFind = assignText.ToCharArray();
            //                    foreach (var c in toFind)
            //                    {
            //                        varEditor.Replace(c, '_');
            //                    }

            FieldControllerBase output;
            try
            {
                output = OperatorScript.Run(_opName, inputs, scope);
                scope.SetVariable(_var.GetVariableName(), output);
            }
            catch (ScriptExecutionException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            }
            return output;
        }

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
