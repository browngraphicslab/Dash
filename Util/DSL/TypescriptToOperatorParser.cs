using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Zu.TypeScript;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    public class TypescriptToOperatorParser
    {
        private static HashSet<string> _currentScriptExecutions = new HashSet<string>();


        public static void TEST()
        {
            TestNumber("7", 7);
            TestNumber("add(3,6)", 9);
            TestString($"\"hello\"","hello");
            TestNumber("add(5,add(div(9,3),mult(2,6)))", 20);

            TestNumber("var a = 8;" +
                       "var b = add(a,2);" +
                       "add(a,b)",18);

            TestNumber("var a = 8 + 3;", 11);
            TestNumber("7 + 9 + 45 + 7", 68);
            

            TestNumber("var a = 8;" +
                       "var b = (6 + 36)/14;" +
                       "((a * b) + 1) * 4", 100);

            TestNumber("var myVar = 6; myVar.myField = 67; 3", 3);
        }


        private static void TestNumber(string script, double correctValue)
        {
            var number = Interpret(script);
            var num = (double)number.GetValue(null);
            Debug.Assert(num.Equals(correctValue));
        }

        private static void TestString(string script, string correctValue)
        {
            var s = Interpret(script);
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
        /// <param name="script"></param>
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
                return "FIXME in OperatorScriptParser";
            var funcName = op.TypedData.First().GetDishName();
            var script = funcName + "(";
            var middle = new List<string>();
            foreach (var inputKey in OperatorScript.GetOrderedKeyControllersForFunction(funcName))
            {
                Debug.Assert(doc.GetField(inputKey) != null);
                middle.Add(DSL.GetScriptForOperatorTree(doc.GetField(inputKey)));
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
        public static FieldControllerBase Interpret(string script, ScriptState state = null)
        {
            var hash = script;//DashShared.UtilShared.GetDeterministicGuid(script);

            if (_currentScriptExecutions.Contains(hash))
            {
                return new TextController(script);
            }

            _currentScriptExecutions.Add(hash);
            try
            {
                //turn script string into function expression
                var se = ParseToExpression(script);
                var exec = se.Execute(state ?? new ScriptState());
                return exec;
            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
            finally
            {
                _currentScriptExecutions.Remove(hash);
            }
        }

        /// <summary>
        /// Method to call to get an operator controller that represents the script called
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static FieldControllerBase GetOperatorControllerForScript(string script, ScriptState state = null)
        {
            try
            {
                var se = ParseToExpression(script);
                return se?.CreateReference(state ?? new ScriptState());

            }
            catch (ScriptException scriptException)
            {
                throw new InvalidDishScriptException(script, scriptException.Error, scriptException);
            }
        }

        private static ScriptExpression ParseToExpression(string script)
        {
            script = script.EndsWith(';') ? script : script + ";";
            var ast = new TypeScriptAST(script);
            var root = ast.RootNode;
            return ParseToExpression(root);
        }


        private static ScriptExpression ParseToExpression(INode node)
        {
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
                case SyntaxKind.OpenBraceToken:
                    break;
                case SyntaxKind.CloseBraceToken:
                    break;
                case SyntaxKind.OpenParenToken:
                    break;
                case SyntaxKind.CloseParenToken:
                    break;
                case SyntaxKind.OpenBracketToken:
                    break;
                case SyntaxKind.CloseBracketToken:
                    break;
                case SyntaxKind.DotToken:
                    break;
                case SyntaxKind.DotDotDotToken:
                    break;
                case SyntaxKind.SemicolonToken:
                    break;
                case SyntaxKind.CommaToken:
                    break;
                case SyntaxKind.LessThanToken:
                    break;
                case SyntaxKind.LessThanSlashToken:
                    break;
                case SyntaxKind.GreaterThanToken:
                    break;
                case SyntaxKind.LessThanEqualsToken:
                    break;
                case SyntaxKind.GreaterThanEqualsToken:
                    break;
                case SyntaxKind.EqualsEqualsToken:
                    break;
                case SyntaxKind.ExclamationEqualsToken:
                    break;
                case SyntaxKind.EqualsEqualsEqualsToken:
                    break;
                case SyntaxKind.ExclamationEqualsEqualsToken:
                    break;
                case SyntaxKind.EqualsGreaterThanToken:
                    break;
                case SyntaxKind.PlusToken:
                    break;
                case SyntaxKind.MinusToken:
                    break;
                case SyntaxKind.AsteriskToken:
                    break;
                case SyntaxKind.AsteriskAsteriskToken:
                    break;
                case SyntaxKind.SlashToken:
                    break;
                case SyntaxKind.PercentToken:
                    break;
                case SyntaxKind.PlusPlusToken:
                    break;
                case SyntaxKind.MinusMinusToken:
                    break;
                case SyntaxKind.LessThanLessThanToken:
                    break;
                case SyntaxKind.GreaterThanGreaterThanToken:
                    break;
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                    break;
                case SyntaxKind.AmpersandToken:
                    break;
                case SyntaxKind.BarToken:
                    break;
                case SyntaxKind.CaretToken:
                    break;
                case SyntaxKind.ExclamationToken:
                    break;
                case SyntaxKind.TildeToken:
                    break;
                case SyntaxKind.AmpersandAmpersandToken:
                    break;
                case SyntaxKind.BarBarToken:
                    break;
                case SyntaxKind.QuestionToken:
                    break;
                case SyntaxKind.ColonToken:
                    break;
                case SyntaxKind.AtToken:
                    break;
                case SyntaxKind.EqualsToken:
                    break;
                case SyntaxKind.PlusEqualsToken:
                    break;
                case SyntaxKind.MinusEqualsToken:
                    break;
                case SyntaxKind.AsteriskEqualsToken:
                    break;
                case SyntaxKind.AsteriskAsteriskEqualsToken:
                    break;
                case SyntaxKind.SlashEqualsToken:
                    break;
                case SyntaxKind.PercentEqualsToken:
                    break;
                case SyntaxKind.LessThanLessThanEqualsToken:
                    break;
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                    break;
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                    break;
                case SyntaxKind.AmpersandEqualsToken:
                    break;
                case SyntaxKind.BarEqualsToken:
                    break;
                case SyntaxKind.CaretEqualsToken:
                    break;
                case SyntaxKind.Identifier:
                    var identifierExpression = node as Identifier;

                    return new VariableExpression(identifierExpression.Text);
                    break;
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
                    break;
                case SyntaxKind.ThrowKeyword:
                    break;
                case SyntaxKind.TrueKeyword:
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
                    break;
                case SyntaxKind.ObjectLiteralExpression:
                    break;
                case SyntaxKind.PropertyAccessExpression:
                    var propAccessExpr = node as PropertyAccessExpression;
                    Debug.Assert(node.Children.Count == 2);

                    var inpDoc = ParseToExpression(propAccessExpr.First);
                    var fieldName = ParseToExpression(propAccessExpr.Last);

                    return new FunctionExpression(DSL.GetFuncName<GetFieldOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                    {
                        {GetFieldOperatorController.InputDocumentKey , inpDoc},
                        {GetFieldOperatorController.KeyNameKey , new LiteralExpression(new TextController((fieldName as VariableExpression).GetVariableName()))},
                    });
                    break;
                case SyntaxKind.ElementAccessExpression:
                    break;
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
                    break;
                case SyntaxKind.FunctionExpression:
                    break;
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
                    break;
                case SyntaxKind.PostfixUnaryExpression:
                    break;
                case SyntaxKind.BinaryExpression:
                    var binaryExpr = node as BinaryExpression;

                    var rightBinExpr = ParseToExpression(binaryExpr.Right);
                    var leftBinExpr = ParseToExpression(binaryExpr.Left);

                    switch (binaryExpr.OperatorToken.Kind)
                    {
                        case SyntaxKind.PlusToken:
                            return new FunctionExpression(DSL.GetFuncName<AddOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                            {
                                {AddOperatorController.AKey,  leftBinExpr},
                                {AddOperatorController.BKey,  rightBinExpr},
                            });
                        case SyntaxKind.MinusToken:
                            return new FunctionExpression(DSL.GetFuncName<SubtractOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                            {
                                {SubtractOperatorController.AKey,  leftBinExpr},
                                {SubtractOperatorController.BKey,  rightBinExpr},
                            });
                        case SyntaxKind.SlashToken:
                            return new FunctionExpression(DSL.GetFuncName<DivideOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                            {
                                {DivideOperatorController.AKey,  leftBinExpr},
                                {DivideOperatorController.BKey,  rightBinExpr},
                            });
                        case SyntaxKind.AsteriskToken:
                            return new FunctionExpression(DSL.GetFuncName<MultiplyOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                            {
                                {MultiplyOperatorController.AKey,  leftBinExpr},
                                {MultiplyOperatorController.BKey,  rightBinExpr},
                            });
                        case SyntaxKind.EqualsToken:
                            if (leftBinExpr is FunctionExpression lefttBinFuncExpr &&
                                lefttBinFuncExpr.GetOperatorName() == DSL.GetFuncName<GetFieldOperatorController>())
                            {
                                return new FunctionExpression(DSL.GetFuncName<SetFieldOperatorController>(), new Dictionary<KeyController, ScriptExpression>()
                                {
                                    {SetFieldOperatorController.InputDocumentKey, lefttBinFuncExpr.GetFuncParams()[GetFieldOperatorController.InputDocumentKey]},
                                    {SetFieldOperatorController.KeyNameKey, lefttBinFuncExpr.GetFuncParams()[GetFieldOperatorController.KeyNameKey]},
                                    {SetFieldOperatorController.FieldValueKey, rightBinExpr},
                                });
                            }
                            throw new Exception("Unknown usage of equals in binary expression");


                        default:
                            throw new Exception("Unkown binary expression type");
                    }

                    break;
                case SyntaxKind.ConditionalExpression:
                    break;
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
                    break;
                case SyntaxKind.VariableStatement:
                    var varStatement = node as VariableStatement;

                    return ParseToExpression(varStatement.DeclarationList);
                    break;
                case SyntaxKind.EmptyStatement:
                    break;
                case SyntaxKind.ExpressionStatement:
                    var exp = (node as ExpressionStatement).Expression;
                    return ParseToExpression(exp);
                    break;
                case SyntaxKind.IfStatement:
                    break;
                case SyntaxKind.DoStatement:
                    break;
                case SyntaxKind.WhileStatement:
                    break;
                case SyntaxKind.ForStatement:
                    break;
                case SyntaxKind.ForInStatement:
                    break;
                case SyntaxKind.ForOfStatement:
                    break;
                case SyntaxKind.ContinueStatement:
                    break;
                case SyntaxKind.BreakStatement:
                    break;
                case SyntaxKind.ReturnStatement:
                    break;
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
                    return new ModifyStateExpression(variableDeclaration.IdentifierStr, ParseToExpression(variableDeclaration.Children[1]));
                    break;
                case SyntaxKind.VariableDeclarationList:
                    var varDeclList = node as VariableDeclarationList;

                    if (varDeclList.Declarations.Count > 1)
                    {
                        return new ExpressionChain(varDeclList.Declarations.Select(ParseToExpression));
                    }

                    //Debug.Assert(varDeclList.Declarations.Any());

                    return ParseToExpression(varDeclList.Declarations[0]);

                    break;
                case SyntaxKind.FunctionDeclaration:
                    break;
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
                        for (int i = 0; i < children.Length - 1; i++)
                        {
                            exprs.Add(ParseToExpression(node.Children[i]));
                        }
                        return new ExpressionChain(exprs);
                    }
                    return ParseToExpression(node.Children.First());
                    break;
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
                    break;
                case SyntaxKind.StringLiteral:
                    var stringLiteral = node as StringLiteral;
                    var parsedString = stringLiteral.Text;
                    return new LiteralExpression(new TextController(parsedString));
                    break;
                case SyntaxKind.CallExpression:
                    var callExpr = node as CallExpression;

                    var parameters = new Dictionary<KeyController, ScriptExpression>();

                    var keys = OperatorScript.GetOrderedKeyControllersForFunction(callExpr.IdentifierStr).ToArray();
                    int keyIndex = 0;
                    foreach (var arg in callExpr.Arguments)
                    {
                        parameters.Add(keys[keyIndex], ParseToExpression(arg));
                        keyIndex++;
                    }

                    var func = new FunctionExpression(callExpr.IdentifierStr, parameters);
                    return func;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return null;
        }

        private class ModifyStateExpression : ScriptExpression
        {
            private string _variableName;
            private ScriptExpression _value;

            public ModifyStateExpression(string variableName, ScriptExpression value)
            {
                Debug.Assert(variableName != null);
                _variableName = variableName;
                _value = value;
            }

            public override FieldControllerBase Execute(ScriptState state)
            {
                var val = _value.Execute(state);
                state.ModifyStateDirectly(_variableName, val);
                return val;
            }

            public override FieldControllerBase CreateReference(ScriptState state)
            {
                throw new NotImplementedException();
                //TODO tfs help with operator/doc stuff
            }

            public override DashShared.TypeInfo Type
            {
                get { return TypeInfo.Any; }
            } //TODO tyler is this correct?
        }
    }
}
