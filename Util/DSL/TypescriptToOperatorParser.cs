using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;
using Zu.TypeScript;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    public class TypescriptToOperatorParser
    {
        private static HashSet<string> _currentScriptExecutions = new HashSet<string>();

        private static bool _undoVar;


        public static void TEST()
        {
            TestNumber("7", 7);
            TestNumber("add(3,6)", 9);
            TestString($"\"hello\"", "hello");
            TestNumber("add(5,add(div(9,3),mult(2,6)))", 20);

            TestNumber("var a = 8;" +
                       "var b = add(a,2);" +
                       "add(a,b)", 18);

            TestNumber("var a = 8 + 3;", 11);
            TestNumber("7 + 9 + 45 + 7", 68);


            TestNumber("var a = 8;" +
                       "var b = (6 + 36)/14;" +
                       "((a * b) + 1) * 4", 100);

            //TestNumber("var myVar = 6; myVar.myField = 67; 3", 3);
        }


        private static async void TestNumber(string script, double correctValue)
        {
            var number = await Interpret(script);
            var num = (double)number.GetValue(null);
            Debug.Assert(num.Equals(correctValue));
        }

        private static async void TestString(string script, string correctValue)
        {
            var s = await Interpret(script);
            Debug.Assert(s.GetValue(null).Equals(correctValue));
        }

        /// <summary>
        /// Public method to call to COMPILE but not Execute a Dish script.
        /// This will return the helpful error message of the invalid script, or NULL if the script compiled correctly.
        /// 
        /// This is slightly faster than actually executing a script so if you are repeatedly checking the validity of a Dish script without needing the return value, call this.
        /// 
        /// AS YOU SHOULD KNOW, JUST BECAUSE IT WILL COMPILE DOESN'T MEAN IT WILL RETURN A VALID VALUE WHEN EXECUTED.   
        /// For instance: add(5, 'hello world') will compile but obviously not return a valid value.
        /// </summary>
        /// <param name = "script" ></ param >
        public static string GetScriptError(string script)
        {
            try
            {
                ParseToExpression(script);
                return null;
            }
            catch (ScriptException scriptException)
            {
                return scriptException.Error.GetHelpfulString();
            }
        }


        public static string GetScriptForOperatorTree(ReferenceController operatorReference, Context context = null)
        {
            var doc = operatorReference.GetDocumentController(context);
            var op = doc.GetDereferencedField<ListController<OperatorController>>(KeyStore.OperatorKey, context);

            if (op == null)
                return $"doc(\"{doc.Id}\").{operatorReference.FieldKey}";
            var opCont = op.TypedData.FirstOrDefault(opController => opController.Outputs.ContainsKey(operatorReference.FieldKey));
            if (opCont == null)
            {
                return $"doc(\"{doc.Id}\").{operatorReference.FieldKey}";
            }

            var funcName = opCont.GetDishName();
            Debug.Assert(funcName != Op.Name.invalid);

            var script = funcName + "(";
            var middle = new List<string>();
            foreach (var inputKey in OperatorScript.GetOrderedKeyControllersForFunction(funcName))
            {
                Debug.Assert(doc.GetField(inputKey) != null);
                var field = doc.GetField(inputKey);
                if (field is ReferenceController refField)
                {
                    middle.Add(GetScriptForOperatorTree(refField, context));
                }
                else
                {
                    middle.Add($"this.{inputKey.Name}");
                }
            }
            return script + string.Join(",", middle) + ")";
        }

        /// <summary>
        /// Method to call to execute a string as a Dish Script and return the FieldController return value.
        /// This method should throw exceptions if the string is not a valid script.
        /// If an InvalidDishScriptException is throw, the exception.ScriptErrorModel SHOULD be a helpful error message
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static async Task<FieldControllerBase> Interpret(string script, Scope scope = null, bool undoVar = false)
        {
            _undoVar = undoVar;

            try
            {
                //turn script string into function expression
                var se = ParseToExpression(script);
                var (field, _) = await se.Execute(scope ?? new Scope());
                return field;
            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
        }

        /// <summary>
        /// Method to call to get an operator controller that represents the script called
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static FieldControllerBase GetOperatorControllerForScript(string script, Scope scope = null)
        {
            try
            {
                var se = ParseToExpression(script);
                return se?.CreateReference(scope ?? new Scope());

            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
        }

        public static ScriptExpression ParseToExpression(string script)
        {
            //this formats string to INode and sends it to below function
            script = script.EndsWith(';') ? script : script + ";";
            var ast = new TypeScriptAST(script);
            var root = ast.RootNode;


            return ParseToExpression(root);
        }

        private static ScriptExpression ParseToExpression(INode node)
        {
            //this converts node to ScriptExpression - most cases call ParseToExpression
            //on individual inner pieces of node

            switch (node.Kind)
            {
            case SyntaxKind.Unknown:
                break;
            case SyntaxKind.EndOfFileToken:
                break;
            case SyntaxKind.SingleLineCommentTrivia:
                break;
            case SyntaxKind.MultiLineCommentTrivia:
                break;
            case SyntaxKind.NewLineTrivia:
                break;
            case SyntaxKind.WhitespaceTrivia:
                break;
            case SyntaxKind.ShebangTrivia:
                break;
            case SyntaxKind.ConflictMarkerTrivia:
                break;
            case SyntaxKind.JsxText:
                break;
            case SyntaxKind.RegularExpressionLiteral:
                break;
            case SyntaxKind.NoSubstitutionTemplateLiteral:
                break;
            case SyntaxKind.TemplateHead:
                break;
            case SyntaxKind.TemplateMiddle:
                break;
            case SyntaxKind.TemplateTail:
                break;
            case SyntaxKind.Identifier:
                var identifierExpression = node as Identifier;

                return new VariableExpression(identifierExpression.Text);
            case SyntaxKind.BreakKeyword:
                break;
            case SyntaxKind.CaseKeyword:
                break;
            case SyntaxKind.CatchKeyword:
                break;
            case SyntaxKind.ClassKeyword:
                break;
            case SyntaxKind.ConstKeyword:
                break;
            case SyntaxKind.ContinueKeyword:
                break;
            case SyntaxKind.DebuggerKeyword:
                break;
            case SyntaxKind.DefaultKeyword:
                break;
            case SyntaxKind.DeleteKeyword:
                break;
            case SyntaxKind.DoKeyword:
                break;
            case SyntaxKind.ElseKeyword:
                break;
            case SyntaxKind.EnumKeyword:
                break;
            case SyntaxKind.ExportKeyword:
                break;
            case SyntaxKind.ExtendsKeyword:
                break;
            case SyntaxKind.FalseKeyword:
                return new LiteralExpression(new BoolController(false));
                break;
            case SyntaxKind.FinallyKeyword:
                break;
            case SyntaxKind.ForKeyword:
                break;
            case SyntaxKind.FunctionKeyword:
                break;
            case SyntaxKind.IfKeyword:
                break;
            case SyntaxKind.ImportKeyword:
                break;
            case SyntaxKind.InKeyword:
                break;
            case SyntaxKind.InstanceOfKeyword:
                break;
            case SyntaxKind.NewKeyword:
                break;
            case SyntaxKind.NullKeyword:
                break;
            case SyntaxKind.ReturnKeyword:
                break;
            case SyntaxKind.SuperKeyword:
                break;
            case SyntaxKind.SwitchKeyword:
                break;
            case SyntaxKind.ThisKeyword:
                return new VariableExpression("this");
            case SyntaxKind.ThrowKeyword:
                break;
            case SyntaxKind.TrueKeyword:
                return new LiteralExpression(new BoolController(true));
                break;
            case SyntaxKind.TryKeyword:
                break;
            case SyntaxKind.TypeOfKeyword:
                break;
            case SyntaxKind.VarKeyword:
                break;
            case SyntaxKind.VoidKeyword:
                break;
            case SyntaxKind.WhileKeyword:
                break;
            case SyntaxKind.WithKeyword:
                break;
            case SyntaxKind.ImplementsKeyword:
                break;
            case SyntaxKind.InterfaceKeyword:
                break;
            case SyntaxKind.LetKeyword:
                break;
            case SyntaxKind.PackageKeyword:
                break;
            case SyntaxKind.PrivateKeyword:
                break;
            case SyntaxKind.ProtectedKeyword:
                break;
            case SyntaxKind.PublicKeyword:
                break;
            case SyntaxKind.StaticKeyword:
                break;
            case SyntaxKind.YieldKeyword:
                break;
            case SyntaxKind.AbstractKeyword:
                break;
            case SyntaxKind.AsKeyword:
                break;
            case SyntaxKind.AnyKeyword:
                break;
            case SyntaxKind.AsyncKeyword:
                break;
            case SyntaxKind.AwaitKeyword:
                break;
            case SyntaxKind.BooleanKeyword:
                break;
            case SyntaxKind.ConstructorKeyword:
                break;
            case SyntaxKind.DeclareKeyword:
                break;
            case SyntaxKind.GetKeyword:
                break;
            case SyntaxKind.IsKeyword:
                break;
            case SyntaxKind.KeyOfKeyword:
                break;
            case SyntaxKind.ModuleKeyword:
                break;
            case SyntaxKind.NamespaceKeyword:
                break;
            case SyntaxKind.NeverKeyword:
                break;
            case SyntaxKind.ReadonlyKeyword:
                break;
            case SyntaxKind.RequireKeyword:
                break;
            case SyntaxKind.NumberKeyword:
                break;
            case SyntaxKind.ObjectKeyword:
                break;
            case SyntaxKind.SetKeyword:
                break;
            case SyntaxKind.StringKeyword:
                break;
            case SyntaxKind.SymbolKeyword:
                break;
            case SyntaxKind.TypeKeyword:
                break;
            case SyntaxKind.UndefinedKeyword:
                break;
            case SyntaxKind.FromKeyword:
                break;
            case SyntaxKind.GlobalKeyword:
                break;
            case SyntaxKind.OfKeyword:
                break;
            case SyntaxKind.QualifiedName:
                break;
            case SyntaxKind.ComputedPropertyName:
                break;
            case SyntaxKind.TypeParameter:
                break;
            case SyntaxKind.Parameter:
                break;
            case SyntaxKind.Decorator:
                break;
            case SyntaxKind.PropertySignature:
                break;
            case SyntaxKind.PropertyDeclaration:
                break;
            case SyntaxKind.MethodSignature:
                break;
            case SyntaxKind.MethodDeclaration:
                break;
            case SyntaxKind.Constructor:
                break;
            case SyntaxKind.GetAccessor:
                break;
            case SyntaxKind.SetAccessor:
                break;
            case SyntaxKind.CallSignature:
                break;
            case SyntaxKind.ConstructSignature:
                break;
            case SyntaxKind.IndexSignature:
                break;
            case SyntaxKind.TypePredicate:
                break;
            case SyntaxKind.TypeReference:
                break;
            case SyntaxKind.FunctionType:
                break;
            case SyntaxKind.ConstructorType:
                break;
            case SyntaxKind.TypeQuery:
                break;
            case SyntaxKind.TypeLiteral:
                break;
            case SyntaxKind.ArrayType:
                break;
            case SyntaxKind.TupleType:
                break;
            case SyntaxKind.UnionType:
                break;
            case SyntaxKind.IntersectionType:
                break;
            case SyntaxKind.ParenthesizedType:
                break;
            case SyntaxKind.ThisType:
                break;
            case SyntaxKind.TypeOperator:
                break;
            case SyntaxKind.IndexedAccessType:
                break;
            case SyntaxKind.MappedType:
                break;
            case SyntaxKind.LiteralType:
                break;
            case SyntaxKind.ObjectBindingPattern:
                break;
            case SyntaxKind.ArrayBindingPattern:
                break;
            case SyntaxKind.BindingElement:
                break;
            case SyntaxKind.ArrayLiteralExpression:
                var arrayChildren = (node as ArrayLiteralExpression)?.Children;
                var parsedList = new List<ScriptExpression>();
                foreach (var element in arrayChildren)
                {
                    parsedList.Add(ParseToExpression(element));
                }
                return new ArrayExpression(new List<ScriptExpression>(parsedList));
            case SyntaxKind.ObjectLiteralExpression:
                var objectProps = (node as ObjectLiteralExpression)?.Children;
                var parsedObList = new List<ScriptExpression>();
                foreach (var element in objectProps)
                {
                    parsedObList.Add(ParseToExpression(element.Children[1]));
                }

                var names = objectProps.Select(n => ((Identifier)n.Children[0]).Text).ToList();
                var dict = new Dictionary<string, ScriptExpression>(names.Zip(parsedObList,
                        (s, expression) => new KeyValuePair<string, ScriptExpression>(s, expression)));

                return new ObjectExpression(dict);
            case SyntaxKind.PropertyAccessExpression:
                var propAccessExpr = node as PropertyAccessExpression;
                Debug.Assert(node.Children.Count == 2);

                var inpDoc = ParseToExpression(propAccessExpr.First);
                var fieldName = ParseToExpression(propAccessExpr.Last);

                return new FunctionExpression(DSL.GetFuncName<GetFieldOperatorController>(), new List<ScriptExpression>
                    {
                        inpDoc,
                        new LiteralExpression(new TextController((fieldName as VariableExpression).GetVariableName()))
                    });
            case SyntaxKind.ElementAccessExpression:
                var elemAcessChildren = (node as ElementAccessExpression)?.Children;
                var elemVar = ParseToExpression(elemAcessChildren?[0]);
                var elemIndex = ParseToExpression(elemAcessChildren?[1]);

                return new FunctionExpression(Op.Name.element_access, new List<ScriptExpression>
                    {
                        elemVar,
                        elemIndex
                    });
            case SyntaxKind.NewExpression:
                break;
            case SyntaxKind.TaggedTemplateExpression:
                break;
            case SyntaxKind.TypeAssertionExpression:
                break;
            case SyntaxKind.ParenthesizedExpression:
                var parenthesizedExpr = node as ParenthesizedExpression;
                Debug.Assert(parenthesizedExpr.Children.Count == 1);
                return ParseToExpression(parenthesizedExpr.Children[0]);
            case SyntaxKind.FunctionExpression:
                var funExpr = (node as Zu.TypeScript.TsTypes.FunctionExpression);

                return new FunctionDeclarationExpression(funExpr.SourceStr, funExpr.Parameters, ParseToExpression(funExpr.Body), TypeInfo.None);
            case SyntaxKind.ArrowFunction:
                break;
            case SyntaxKind.DeleteExpression:
                break;
            case SyntaxKind.TypeOfExpression:
                break;
            case SyntaxKind.VoidExpression:
                break;
            case SyntaxKind.AwaitExpression:
                break;
            case SyntaxKind.PrefixUnaryExpression:
                var preUnEx = (PrefixUnaryExpression)node;
                var body = ParseToExpression(preUnEx.Children[0]);
                switch (preUnEx.Operator)
                {
                case SyntaxKind.MinusToken:
                    return new FunctionExpression(Op.Name.operator_negate, new List<ScriptExpression> { body });
                }
                break;
            case SyntaxKind.PostfixUnaryExpression:
                var postUnEx = (PostfixUnaryExpression)node;
                var res = postUnEx.Children[0].GetText();
                switch (postUnEx.Operator)
                {
                case SyntaxKind.PlusPlusToken:
                    return ParseToExpression(res + " = " + res + " + 1");
                case SyntaxKind.MinusMinusToken:
                    return ParseToExpression(res + " = " + res + " - 1");
                default:
                    return null;
                }
            case SyntaxKind.BinaryExpression:
                var binaryExpr = node as BinaryExpression;

                ScriptExpression rightBinExpr = ParseToExpression(binaryExpr?.Right);
                ScriptExpression leftBinExpr = ParseToExpression(binaryExpr.Left);

                switch (binaryExpr.OperatorToken.Kind)
                {
                case SyntaxKind.PlusToken:
                    return new FunctionExpression(Op.Name.operator_add, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.MinusToken:
                    return new FunctionExpression(Op.Name.operator_subtract, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.SlashToken:
                    return new FunctionExpression(Op.Name.operator_divide, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.AsteriskToken:
                    return new FunctionExpression(Op.Name.operator_multiply, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.GreaterThanToken:
                    return new FunctionExpression(Op.Name.operator_greater_than, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.LessThanToken:
                    return new FunctionExpression(Op.Name.operator_less_than, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.GreaterThanEqualsToken:
                    return new FunctionExpression(Op.Name.operator_greater_than_equals, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.LessThanEqualsToken:
                    return new FunctionExpression(Op.Name.operator_less_than_equals, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.EqualsEqualsToken:
                    return new FunctionExpression(Op.Name.operator_equal, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.ExclamationEqualsToken:
                    return new FunctionExpression(Op.Name.operator_not_equal, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.PercentToken:
                    return new FunctionExpression(Op.Name.operator_modulo, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.CaretToken:
                    return new FunctionExpression(Op.Name.operator_exponential, new List<ScriptExpression>
                            {
                                leftBinExpr,
                                rightBinExpr
                            });
                case SyntaxKind.EqualsToken:
                    switch (leftBinExpr)
                    {
                    case FunctionExpression lefttBinFuncExpr when lefttBinFuncExpr.GetOperatorName() == DSL.GetFuncName<GetFieldOperatorController>():
                        return new FunctionExpression(DSL.GetFuncName<SetFieldOperatorController>(), new List<ScriptExpression>
                                    {
                                    lefttBinFuncExpr.GetFuncParams()[0],
                                    lefttBinFuncExpr.GetFuncParams()[1],
                                    rightBinExpr
                                });
                    case FunctionExpression lefttBinFuncExpr2 when lefttBinFuncExpr2.GetOperatorName() == DSL.GetFuncName<ElementAccessOperatorController>():
                        return new FunctionExpression(DSL.GetFuncName<SetListFieldOperatorController>(), new List<ScriptExpression>
                                    {
                                    new LiteralExpression(new TextController((lefttBinFuncExpr2.GetFuncParams()[0] as VariableExpression)?.GetVariableName() )),
                                    lefttBinFuncExpr2.GetFuncParams()[0],
                                    lefttBinFuncExpr2.GetFuncParams()[1],
                                    rightBinExpr
                                });
                    case VariableExpression safeBinExpr:
                        return new VariableAssignmentExpression(safeBinExpr.GetVariableName(), rightBinExpr, _undoVar);
                    }
                    throw new Exception("Unknown usage of equals in binary expression");
                case SyntaxKind.PlusEqualsToken:
                    if (leftBinExpr is VariableExpression varExpAdd) return new SelfRefAssignmentExpression(varExpAdd, rightBinExpr, Op.Name.operator_add);
                    break;
                case SyntaxKind.MinusEqualsToken:
                    if (leftBinExpr is VariableExpression varExpSubtract) return new SelfRefAssignmentExpression(varExpSubtract, rightBinExpr, Op.Name.operator_subtract);
                    break;
                case SyntaxKind.AsteriskEqualsToken:
                    if (leftBinExpr is VariableExpression varExpMultiply) return new SelfRefAssignmentExpression(varExpMultiply, rightBinExpr, Op.Name.operator_multiply);
                    break;
                case SyntaxKind.SlashEqualsToken:
                    if (leftBinExpr is VariableExpression varExpDivide) return new SelfRefAssignmentExpression(varExpDivide, rightBinExpr, Op.Name.operator_divide);
                    break;
                case SyntaxKind.PercentEqualsToken:
                    if (leftBinExpr is VariableExpression varExpMod) return new SelfRefAssignmentExpression(varExpMod, rightBinExpr, Op.Name.operator_modulo);
                    break;
                case SyntaxKind.CaretEqualsToken:
                    if (leftBinExpr is VariableExpression varExpExp) return new SelfRefAssignmentExpression(varExpExp, rightBinExpr, Op.Name.operator_exponential);
                    break;
                default:
                    throw new Exception("Unkown binary expression type");
                }
                break;
            case SyntaxKind.ConditionalExpression:
                var cNode = (ConditionalExpression) node;

                return new TernaryExpression(ParseToExpression(cNode.Condition), ParseToExpression(cNode.WhenTrue), ParseToExpression(cNode.WhenFalse));
            case SyntaxKind.TemplateExpression:
                break;
            case SyntaxKind.YieldExpression:
                break;
            case SyntaxKind.SpreadElement:
                break;
            case SyntaxKind.ClassExpression:
                break;
            case SyntaxKind.OmittedExpression:
                break;
            case SyntaxKind.ExpressionWithTypeArguments:
                break;
            case SyntaxKind.AsExpression:
                break;
            case SyntaxKind.NonNullExpression:
                break;
            case SyntaxKind.MetaProperty:
                break;
            case SyntaxKind.TemplateSpan:
                break;
            case SyntaxKind.SemicolonClassElement:
                break;
            case SyntaxKind.Block:
                //parse each child
                var blocChil = ((Block) node).Children;
                var expressions = new List<ScriptExpression>();
                foreach (var child in blocChil)
                {
                    expressions.Add(ParseToExpression(child));
                }
                return new ExpressionChain(expressions);
            case SyntaxKind.VariableStatement:
                var varStatement = node as VariableStatement;

                return ParseToExpression(varStatement.DeclarationList);
            case SyntaxKind.EmptyStatement:
                //return empty string
                break;
            //return new FunctionExpression(Op.Name.invalid, new List<ScriptExpression>());
            case SyntaxKind.ExpressionStatement:
                var exp = (node as ExpressionStatement).Expression;
                return ParseToExpression(exp);
            case SyntaxKind.IfStatement:
                var ifChild = (node as IfStatement).Children;

                var ifBinary = ParseToExpression(ifChild[0]);
                var ifBlock = ParseToExpression(ifChild[1]);
                var elseBlock = ifChild.Count > 2 ? ParseToExpression(ifChild[2]) : null;

                return new IfExpression(DSL.GetFuncName<IfOperatorController>(), new Dictionary<KeyController, ScriptExpression>
                    {
                                {IfOperatorController.BoolKey,  ifBinary},
                                {IfOperatorController.IfBlockKey,  ifBlock},
                                {IfOperatorController.ElseBlockKey,  elseBlock}
                            });

            case SyntaxKind.DoStatement:
                var doStatement = (node as DoStatement).Children;
                var doBlock = ParseToExpression(doStatement[0]);
                var doBinary = ParseToExpression(doStatement[1]);

                List<ScriptExpression> outputs = new List<ScriptExpression>();
                outputs.Add(doBlock);
                outputs.Add(new WhileExpression(DSL.GetFuncName<WhileOperatorController>(), new Dictionary<KeyController, ScriptExpression>
                    {
                        {WhileOperatorController.BoolKey,  doBinary},
                        {WhileOperatorController.BlockKey,  doBlock}
                    }));
                return new ExpressionChain(outputs);
            case SyntaxKind.WhileStatement:
                var whilChild = (node as WhileStatement).Children;
                Debug.Assert(whilChild.Count == 2);

                var whilBinary = ParseToExpression(whilChild[0]);
                var whilBlock = ParseToExpression(whilChild[1]);

                //  make a while operator and call it in this function
                return new WhileExpression(Op.Name.while_lp, new Dictionary<KeyController, ScriptExpression>
                    {
                        {WhileOperatorController.BoolKey,  whilBinary},
                        {WhileOperatorController.BlockKey,  whilBlock}
                    });
            case SyntaxKind.ForStatement:
                var forChild = (node as ForStatement)?.Children;
                var countDeclaration = ParseToExpression(forChild?[0]);
                var forBinary = ParseToExpression(forChild?[1]);
                var forIncrement = ParseToExpression(forChild?[2]);
                var forBody = ParseToExpression(forChild?[3]) as ExpressionChain;

                return new ForExpression(Op.Name.for_lp, countDeclaration, forBinary, forIncrement, forBody);
            case SyntaxKind.ForInStatement:
                var forInChild = (node as ForInStatement)?.Children;

                var subVarName = forInChild?[0].First.IdentifierStr;
                var listNameExpr = ParseToExpression(forInChild?[1]);
                var forInBody = ParseToExpression(forInChild?[2]) as ExpressionChain;

                return new ForInExpression(Op.Name.for_in_lp, subVarName, listNameExpr, forInBody);
            case SyntaxKind.ForOfStatement:
                break;
            case SyntaxKind.ContinueStatement:
                break;
            case SyntaxKind.BreakStatement:
                // Break doesn't currently work properly, all it does is terminate one expression chain,
                // needs to terminate enclosing loop/end statement
                return new BreakLoopExpression();
            case SyntaxKind.ReturnStatement:
                //as it is right now, return is kind of hacky, if this line is still here, it means that
                //return still works by outputting an empty text controller if it isn't called, and is storing
                //itself as a variable- if we could find a way to break out of the recursive loop that would 
                //be a lot more elegant instead of using an error statement
                return new ReturnExpression(ParseToExpression(node.Children[0]));
            case SyntaxKind.WithStatement:
                break;
            case SyntaxKind.SwitchStatement:
                break;
            case SyntaxKind.LabeledStatement:
                break;
            case SyntaxKind.ThrowStatement:
                break;
            case SyntaxKind.TryStatement:
                break;
            case SyntaxKind.DebuggerStatement:
                break;
            case SyntaxKind.VariableDeclaration:
                var variableDeclaration = node as VariableDeclaration;

                return new VariableDeclarationExpression(variableDeclaration.IdentifierStr, ParseToExpression(variableDeclaration.Children[1]), _undoVar);
            case SyntaxKind.VariableDeclarationList:
                var varDeclList = node as VariableDeclarationList;

                if (varDeclList.Declarations.Count > 1)
                {
                    return new ExpressionChain(varDeclList.Declarations.Select(ParseToExpression), false);
                }

                //Debug.Assert(varDeclList.Declarations.Any());

                return ParseToExpression(varDeclList.Declarations[0]);
            case SyntaxKind.FunctionDeclaration:
                var funDec = (node as FunctionDeclaration);

                var declarationExpression = new FunctionDeclarationExpression(funDec.Body.GetText(), funDec.Parameters, ParseToExpression(funDec.Body), TypeInfo.None);

                if (funDec.IdentifierStr == null)
                {
                    return declarationExpression;
                }
                else
                {
                    return new VariableDeclarationExpression(funDec.IdentifierStr, declarationExpression, _undoVar);
                }
            case SyntaxKind.ClassDeclaration:
                break;
            case SyntaxKind.InterfaceDeclaration:
                break;
            case SyntaxKind.TypeAliasDeclaration:
                break;
            case SyntaxKind.EnumDeclaration:
                break;
            case SyntaxKind.ModuleDeclaration:
                break;
            case SyntaxKind.ModuleBlock:
                break;
            case SyntaxKind.CaseBlock:
                break;
            case SyntaxKind.NamespaceExportDeclaration:
                break;
            case SyntaxKind.ImportEqualsDeclaration:
                break;
            case SyntaxKind.ImportDeclaration:
                break;
            case SyntaxKind.ImportClause:
                break;
            case SyntaxKind.NamespaceImport:
                break;
            case SyntaxKind.NamedImports:
                break;
            case SyntaxKind.ImportSpecifier:
                break;
            case SyntaxKind.ExportAssignment:
                break;
            case SyntaxKind.ExportDeclaration:
                break;
            case SyntaxKind.NamedExports:
                break;
            case SyntaxKind.ExportSpecifier:
                break;
            case SyntaxKind.MissingDeclaration:
                break;
            case SyntaxKind.ExternalModuleReference:
                break;
            case SyntaxKind.JsxElement:
                break;
            case SyntaxKind.JsxSelfClosingElement:
                break;
            case SyntaxKind.JsxOpeningElement:
                break;
            case SyntaxKind.JsxClosingElement:
                break;
            case SyntaxKind.JsxAttribute:
                break;
            case SyntaxKind.JsxAttributes:
                break;
            case SyntaxKind.JsxSpreadAttribute:
                break;
            case SyntaxKind.JsxExpression:
                break;
            case SyntaxKind.CaseClause:
                break;
            case SyntaxKind.DefaultClause:
                break;
            case SyntaxKind.HeritageClause:
                break;
            case SyntaxKind.CatchClause:
                break;
            case SyntaxKind.PropertyAssignment:
                var propAssign = (node as PropertyAssignment);
                break;
            case SyntaxKind.ShorthandPropertyAssignment:
                break;
            case SyntaxKind.SpreadAssignment:
                break;
            case SyntaxKind.EnumMember:
                break;
            case SyntaxKind.SourceFile:
                if (node.Children.Count > 2)
                {
                    var children = node.Children.ToArray();
                    var exprs = new List<ScriptExpression>();
                    for (var i = 0; i < children.Length - 1; i++)
                    {
                        var expr = ParseToExpression(node.Children[i]);
                        if (expr != null)
                        {
                            exprs.Add(expr);
                        }
                    }
                    return new ExpressionChain(exprs, false);
                }
                return ParseToExpression(node.Children.First());
            case SyntaxKind.Bundle:
                break;
            case SyntaxKind.JsDocTypeExpression:
                break;
            case SyntaxKind.JsDocAllType:
                break;
            case SyntaxKind.JsDocUnknownType:
                break;
            case SyntaxKind.JsDocArrayType:
                break;
            case SyntaxKind.JsDocUnionType:
                break;
            case SyntaxKind.JsDocTupleType:
                break;
            case SyntaxKind.JsDocNullableType:
                break;
            case SyntaxKind.JsDocNonNullableType:
                break;
            case SyntaxKind.JsDocRecordType:
                break;
            case SyntaxKind.JsDocRecordMember:
                break;
            case SyntaxKind.JsDocTypeReference:
                break;
            case SyntaxKind.JsDocOptionalType:
                break;
            case SyntaxKind.JsDocFunctionType:
                break;
            case SyntaxKind.JsDocVariadicType:
                break;
            case SyntaxKind.JsDocConstructorType:
                break;
            case SyntaxKind.JsDocThisType:
                break;
            case SyntaxKind.JsDocComment:
                break;
            case SyntaxKind.JsDocTag:
                break;
            case SyntaxKind.JsDocAugmentsTag:
                break;
            case SyntaxKind.JsDocParameterTag:
                break;
            case SyntaxKind.JsDocReturnTag:
                break;
            case SyntaxKind.JsDocTypeTag:
                break;
            case SyntaxKind.JsDocTemplateTag:
                break;
            case SyntaxKind.JsDocTypedefTag:
                break;
            case SyntaxKind.JsDocPropertyTag:
                break;
            case SyntaxKind.JsDocTypeLiteral:
                break;
            case SyntaxKind.JsDocLiteralType:
                break;
            case SyntaxKind.SyntaxList:
                break;
            case SyntaxKind.NotEmittedStatement:
                break;
            case SyntaxKind.PartiallyEmittedExpression:
                break;
            case SyntaxKind.MergeDeclarationMarker:
                break;
            case SyntaxKind.EndOfDeclarationMarker:
                break;
            case SyntaxKind.Count:
                break;
            case SyntaxKind.NumericLiteral:
                var numberLiteral = node as NumericLiteral;
                double parsedNumber;
                try
                {
                    parsedNumber = double.Parse(numberLiteral.Text);
                }
                catch (OverflowException overflow)
                {
                    parsedNumber = double.PositiveInfinity;
                }

                return new LiteralExpression(new NumberController(parsedNumber));
            case SyntaxKind.StringLiteral:
                var stringLiteral = node as StringLiteral;
                var parsedString = stringLiteral.Text;
                return new LiteralExpression(new TextController(parsedString));
            case SyntaxKind.CallExpression:
                var callExpr = node as CallExpression;
                var parameters = new List<ScriptExpression>();
                INode callFunc = callExpr.Expression;
                var type = callExpr.Expression.Kind;

                if (type == SyntaxKind.PropertyAccessExpression)
                {
                    parameters.Add(ParseToExpression(((PropertyAccessExpression)callFunc).First));
                    callFunc = ((PropertyAccessExpression)callFunc).Last;
                }


                foreach (var arg in callExpr.Arguments)
                {
                    parameters.Add(ParseToExpression(arg));
                }

                var func = new FunctionExpression(parameters, ParseToExpression(callFunc));
                return func;

            default:
                throw new ArgumentOutOfRangeException();
            }
            return null;
        }
    }
}
