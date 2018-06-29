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
        private readonly SelfRefAssignment _selfRefOp;

        public SelfRefAssignmentExpression(VariableExpression var, ScriptExpression assignExp, SelfRefAssignment selfRefOp)
        {
            _var = var;
            _assignExp = assignExp;
            _selfRefOp = selfRefOp;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var varCtrl = _var.Execute(scope);
            var assignCtrl = _assignExp.Execute(scope);

            var leftKey = BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>.LeftKey;
            var rightKey = BinaryOperatorControllerBase<FieldControllerBase, FieldControllerBase>.RightKey;
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
            //                case SelfRefAssignment.Subtraction:
            //                    var varEditor = new StringBuilder(varText);
            //                    var toRemove = assignText.ToCharArray();
            //                    foreach (var c in toRemove)
            //                    {
            //                        var index = varEditor.ToString().IndexOf(c);
            //                        if (index != -1) varEditor.Remove(index, 1);
            //                    }
            //                    outString = varEditor.ToString(); break;
            //                case SelfRefAssignment.Division:
            //                    varEditor = new StringBuilder(varText);
            //                    toRemove = assignText.ToCharArray();
            //                    foreach (var c in toRemove)
            //                    {
            //                        varEditor.Replace(c.ToString(), "");
            //                    }
            //                    outString = varEditor.ToString(); break;
            //                case SelfRefAssignment.StringSearch:
            //                    varEditor = new StringBuilder(varText);
            //                    var toFind = assignText.ToCharArray();
            //                    foreach (var c in toFind)
            //                    {
            //                        varEditor.Replace(c, '_');
            //                    }
            //                    outString = varEditor.ToString(); break;
            //            }

            //            scope.SetVariable(_varName, new TextController(outString));
            //            break;
            //    }
            //}

            string opName = "";
            switch (_selfRefOp)
            {
                case SelfRefAssignment.Addition:
                    opName = "add";
                    break;
                case SelfRefAssignment.Subtraction:
                    opName = "subtract";
                    break;
                case SelfRefAssignment.Multiplication:
                    opName = "mult";
                    break;
                case SelfRefAssignment.Division:
                    opName = "div";
                    break;
                case SelfRefAssignment.Modulo:
                    opName = "mod";
                    break;
            }

            if (String.IsNullOrEmpty(opName))
            {
                Debug.Fail("How did you get here?");
            }

            FieldControllerBase output;
            try
            {
                output = OperatorScript.Run(opName, inputs, scope);
                scope.SetVariable(_var.GetVariableName(), output);
            }
            catch (ScriptExecutionException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(opName));
            }
            return output;
        }

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        //TODO
        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType("add");
    }
}
