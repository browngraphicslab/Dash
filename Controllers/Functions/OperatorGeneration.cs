using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using CsvHelper.Configuration.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dash
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OperatorReturnNameAttribute : Attribute
    {
        public string Name { get; }
        public OperatorReturnNameAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OperatorFunctionNameAttribute : Attribute
    {
        public string[] Names { get; }
        public OperatorFunctionNameAttribute(params string[] names)
        {
            Names = names;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class GeneratorIgnoreAttribute : Attribute
    {
    }

#if false
    public class Test
    {
        public void Process()
        {
            var s = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class Aritmetic
    {

        public static void Sub(NumberController a, NumberController b)
        {

        }

        public static Task SubtractTwoNumbers(NumberController a, NumberController b)
        {
            return Task.CompletedTask;
        }

        [OperatorReturnName(""Sum"")]
        public static NumberController Add(NumberController a, NumberController b)
        {
            return new NumberController(a.Data + b.Data);
        }

        public static (NumberController quotient, NumberController remainder) Divide(NumberController a, NumberController b)
        {
            return (new NumberController(a.Data / b.Data), new NumberController(a.Data % b.Data));
        }

        [OperatorReturnName(""Sum"")]
        public static Task<NumberController> Multiply(NumberController a, NumberController b)
        {
            return Task.FromResult(new NumberController(a.Data + b.Data));
        }

        public static Task<(NumberController quotient, NumberController remainder)> Subtract(NumberController a, NumberController b)
        {
            return Task.FromResult((new NumberController(a.Data / b.Data), new NumberController(a.Data % b.Data)));
        }
    }
}
";
            //var fileTrees = Directory.EnumerateFiles(Path.Combine(/*Path.GetDirectoryName(Host.TemplateFile)*/"", "Operators"))
            //    .Where(f => Path.GetExtension(f) == ".cs")
            //    .Select(f => (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(f)));
            var fileTrees = new List<SyntaxTree>{(CSharpSyntaxTree)CSharpSyntaxTree.ParseText(s)};
            foreach (var tree in fileTrees)
            {
                var root = (CompilationUnitSyntax)tree.GetRoot();
                var funcs = new List<(string functionName, string scriptName, bool isAsync,
                    List<(string name, string type, bool required)>,
                    List<(string name, string type)>)>();
                foreach (var methodDeclarationSyntax in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    string methodNamespace = "";
                    SyntaxNode node = methodDeclarationSyntax.Parent;
                    while (node != null)
                    {
                        if (node is ClassDeclarationSyntax classSyntax)
                        {
                            methodNamespace = classSyntax.Identifier.ValueText + "." + methodNamespace;
                        } else if (node is NamespaceDeclarationSyntax ns)
                        {
                            if (ns.Name is IdentifierNameSyntax ident)
                            {
                                methodNamespace = ident.Identifier.ValueText + "." + methodNamespace;
                            } else if (ns.Name is QualifiedNameSyntax qualified)
                            {
                            }
                        }
                        else
                        {
                            break;
                        }

                        node = node.Parent;
                    }
                    var attribs = methodDeclarationSyntax.AttributeLists;
                    var name = methodDeclarationSyntax.Identifier.ValueText;
                    var retType = methodDeclarationSyntax.ReturnType;
                    var paramList = methodDeclarationSyntax.ParameterList;
                    var inputs = ParseInputs(paramList);
                    var (outputs, isAsync) = ParseOutputs(retType, attribs);

                    funcs.Add((name, name, isAsync, inputs, outputs));
                }
            }
        }

        private List<(string name, string type, bool required)> ParseInputs(ParameterListSyntax parameterList)
        {
            var inputs = new List<(string name, string type, bool required)>();
            foreach (var parameter in parameterList.Parameters)
            {
                var t = parameter.Type;
                if (!(t is IdentifierNameSyntax ident))
                {
                    return null;
                }

                var name = parameter.Identifier.ValueText;
                inputs.Add((name, ident.Identifier.ValueText, parameter.Default == null));
            }

            return inputs;
        }

        private (List<(string name, string type)> outputs, bool isAsync) ParseOutputs(TypeSyntax retType, SyntaxList<AttributeListSyntax> attribs)
        {
            var outputs = new List<(string name, string type)>();

            switch (retType)
            {
            case PredefinedTypeSyntax predefined:
                if (predefined.Keyword.ValueText == "void")
                {
                    return (outputs, false);
                }
                else
                {
                    return (null, false);
                }
            case IdentifierNameSyntax ident:
                {
                    var type = ident.Identifier.ValueText;
                    if (type == "Task")
                    {
                        return (outputs, true);
                    }

                    outputs.Add((GetName(attribs, 1)[0], type));
                    break;
                }
            case TupleTypeSyntax tuple:
                {
                    foreach (var tupleElementSyntax in tuple.Elements)
                    {
                        var name = tupleElementSyntax.Identifier.ValueText;
                        var (list, _) = ParseOutputs(tupleElementSyntax.Type, attribs);
                        if (list.Count != 1)
                        {
                            return (null, false);
                        }
                        outputs.Add((name, list[0].type));
                    }

                    break;
                }
            case GenericNameSyntax generic:
                {
                    var name = generic.Identifier.ValueText;
                    if (name == "Task")
                    {
                        if (generic.TypeArgumentList.Arguments.Count == 1)
                        {
                            var (list, _) = ParseOutputs(generic.TypeArgumentList.Arguments[0], attribs);
                            return (list, true);
                        }
                        else
                        {
                            return (null, false);
                        }
                    }
                    else
                    {
                        outputs.Add((GetName(attribs, 1)[0], generic.ToString()));
                    }

                        break;
                }
            default:
                return (null, false);
            }
            return (outputs, false);
        }

        private string[] GetName(SyntaxList<AttributeListSyntax> attribs, int numParams)
        {
            var a = new string[numParams];
            int current = 0;
            foreach (var attributeListSyntax in attribs)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var attName = attributeSyntax.Name;
                    if (attName is IdentifierNameSyntax ident)
                    {
                        var identName = ident.Identifier.ValueText;
                        if (identName == "OperatorReturnName")
                        {
                            var args = attributeSyntax.ArgumentList.Arguments;
                            if (args.Count == 1)
                            {
                                var arg = args[0];
                                if (arg.Expression is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
                                {
                                    var token = literal.Token.ValueText;
                                    a[current++] = token;
                                    if (current == numParams)
                                    {
                                        return a;
                                    }
                                }
                            }

                        }
                    }

                }
                
            }

            for (; current < numParams; ++current)
            {
                a[current] = $"output{current}";
            }

            return a;
        }
    }
#endif
}
