using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DashShared;
using TypeInfo = DashShared.TypeInfo;

namespace Dash
{
    public class OperatorScript
    {
        public static OperatorScript Instance = new OperatorScript();
        private static Dictionary<Op.Name, List<OperatorControllerOverload>> _functionMap;
        private static Dictionary<Type, Op.Name> _reverseFunctionMap;

        private static bool PrintAllFuncDocumentation;
        public static string FunctionDocumentation;

        private OperatorScript()
        {
            FunctionDocumentation = "";
            PrintAllFuncDocumentation = false;
            Init();
        }

        public void Init()
        {
            if (PrintAllFuncDocumentation)
            {
                Debug.WriteLine("\n\n\n\nAll DSL Functions: \n");
            }

            _functionMap = new Dictionary<Op.Name, List<OperatorControllerOverload>>();
            _reverseFunctionMap = new Dictionary<Type, Op.Name>();
            foreach (var operatorType in GetTypesWithOperatorAttribute(Assembly.GetExecutingAssembly()))
            {
                //IF YOU CRASHED ON THIS LINE THEN YOU PROBABLY ADDED A NEW OPERATOR WITHOUT AN EMPTY CONSTRUCTOR. 
                OperatorController op = (OperatorController)Activator.CreateInstance(operatorType);

                var typeNames = operatorType.GetCustomAttribute<OperatorTypeAttribute>().GetTypeNames();

                foreach (var typeName in typeNames)
                {
                    if (_functionMap.ContainsKey(typeName))
                    {
                        _functionMap[typeName].Add(new OperatorControllerOverload(op.Inputs.ToList(), operatorType));
                    }
                    else
                    {
                        var list = new List<OperatorControllerOverload>
                        {
                            new OperatorControllerOverload(op.Inputs.ToList(), operatorType)
                        };
                        _functionMap[typeName] = list;
                    }
                    _reverseFunctionMap[operatorType] = typeName;


                    if (PrintAllFuncDocumentation)
                    {
                        PrintDocumentation(typeName.ToString(), op);
                    }
                }

            }

            DishReplView.SetDataset(GetDataset());

            if (PrintAllFuncDocumentation)
            {
                Debug.WriteLine("\n\n\n\n\n");
            }
        }

        public static List<string> GetDataset() => _functionMap.Keys.Select(k => k.ToString()).ToList();

        public static IEnumerable<OperatorControllerOverload> GetOverloadsFor(Op.Name funcName) => _functionMap[funcName];

        public static ListController<TextController> GetFunctionList()
        {
            var functionNames = new ListController<TextController>(_functionMap.Select(k => new TextController("     " + k.Key.ToString())).ToList().OrderBy(t => t.Data));
            const string alphString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            CharEnumerator alphabet = alphString.GetEnumerator();
            alphabet.MoveNext();
            functionNames.Insert(0, new TextController(alphabet.Current.ToString()));
            for (var index = 1; index < functionNames.Count; index++)
            {
                string name = functionNames[index].Data;
                string alphaCurr = alphabet.Current.ToString();
                string alphaCurrLow = alphaCurr.ToLower();
                var startInd = 5;
                string firstLet = name[startInd].ToString();
                while (!alphString.ToLower().Contains(firstLet))
                {
                    startInd++;
                    firstLet = name[startInd].ToString();
                }
                if (alphaCurr.Equals(firstLet) || alphaCurrLow.Equals(firstLet)) continue;
                while (!alphaCurr.Equals(firstLet) && !alphaCurrLow.Equals(firstLet))
                {
                    alphabet.MoveNext();
                    alphaCurr = alphabet.Current.ToString();
                    alphaCurrLow = alphaCurr.ToLower();
                }
                functionNames.Insert(index, new TextController(alphabet.Current.ToString()));
                index++;
            }
            alphabet.Dispose();
            return functionNames;
        }

        public static string GetStringFormattedTypeListsFor(Op.Name functionName)
        {
            var typeSublists = new List<KeyValuePair<int, string>>();
            foreach (var overload in _functionMap[functionName])
            {
                var typeInfoList = overload.ParamTypes.Select(kv => kv.Value.Type).ToList();
                var numParams = typeInfoList.Count;
                typeSublists.Add(new KeyValuePair<int, string>(numParams, $"\n            ({string.Join(", ", typeInfoList)})"));
            }

            var sortedParams = typeSublists.OrderBy(x => x.Key).ToList();
            return string.Join("", sortedParams.Select(kv => kv.Value).ToList());
        }

        private static void PrintDocumentation(string funcName, OperatorController op)
        {
            var doc = op.Outputs[0].Value.ToString() + "   " + funcName + "( " + string.Join(',', op.Inputs.Select(i => " " + i.Value.Type.ToString() + "  " + i.Key.Name.ToLower())) + " );";
            FunctionDocumentation += doc + "         \n";
            Debug.WriteLine(doc);
        }

        /// <summary>
        /// gets all the keys of a function's parameters but as a dictionary of key name to key controller
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static Dictionary<string, KeyController> GetKeyControllersForFunction(Op.Name funcName)
        {
            return GetOrderedKeyControllersForFunction(funcName).ToDictionary(k => k.Name, v => v);
        }

        /// <summary>
        /// returns the dish function name for a given operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Op.Name GetDishOperatorName<T>() where T : OperatorController
        {
            var t = typeof(T);

            //if this fails then the function name doens't exist for the given controller
            Debug.Assert(_reverseFunctionMap.ContainsKey(t));

            return _reverseFunctionMap.ContainsKey(t) ? _reverseFunctionMap[t] : Op.Name.invalid;
        }

        public static OperatorController GetOperatorWithName(Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;
            //TODO With overloading this isn't correct
            var t = _functionMap[funcName].OrderBy(x => x.ParamTypes.Count).First().OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            return op;
        }

        /// <summary>
        /// returns the dish function name for a given operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Op.Name GetDishOperatorName<T>(T controller) where T : OperatorController
        {
            //if this fails then the function name doens't exist for the given controller

            return controller.GetType().GetCustomAttribute<OperatorTypeAttribute>().GetTypeNames().First();
        }


        /// <summary>
        /// returns whether a certain function exists based on a string name
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static bool FuncNameExists(Op.Name funcName) => _functionMap.ContainsKey(funcName);

        //TODO With overloads we need more info to have this make sense
        public static DashShared.TypeInfo GetOutputType(Op.Name funcName)
        {
            if (funcName == Op.Name.invalid) return TypeInfo.None;
            return GetOperatorWithName(funcName)?.Outputs?.ElementAt(0).Value ?? DashShared.TypeInfo.None;
        }

        //TODO With overloads we need more info to have this make sense
        public static DashShared.TypeInfo GetFirstInputType(Op.Name funcName)
        {
            return GetOperatorWithName(funcName)?.Inputs?.ElementAt(0).Value.Type ?? DashShared.TypeInfo.None;
        }

        public static List<TypeInfo> GetDefaultInputTypeListFor(Op.Name funcName) => GetOperatorWithName(funcName)?.Inputs.ToList().Select(kv => kv.Value.Type).ToList();

        public static bool IsOverloaded(Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return false;
            var overloads = _functionMap[funcName].OrderBy(x => x.ParamTypes.Count).ToList();
            if (overloads.Count == 1) return false;
            return overloads[0].ParamTypes.Count == overloads[1].ParamTypes.Count;
        }


        /// <summary>
        /// returns an ordered list of the keycontorllers in a function
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static List<KeyController> GetOrderedKeyControllersForFunction(Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;

            //TODO With overloading this isn't correct
            var t = _functionMap[funcName].First().OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            return op.Inputs.ToList().Select(i => i.Key).ToList();
        }


        public static Dictionary<KeyController, IOInfo> GetKeyControllerDictionaryForFunction(Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;

            //TODO With overloading this isn't correct
            var t = _functionMap[funcName].First().OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            return op.Inputs.ToDictionary(k => k.Key, v => v.Value);
        }

        private static IEnumerable<Type> GetTypesWithOperatorAttribute(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(OperatorTypeAttribute), true).Length == 0 || type.IsAbstract) continue;
                Activator.CreateInstance(type);
                yield return type;
            }
        }

        public static Task<FieldControllerBase> Run(Op.Name funcName, List<FieldControllerBase> args, Scope scope = null)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;

            var overloads = _functionMap[funcName];

            var distances = new List<KeyValuePair<OperatorControllerOverload, List<int>>>();
            foreach (var overload in overloads)
            {
                var dist = overload.GetDistances(args);
                if (dist == null) continue;

                dist.Sort();
                distances.Add(new KeyValuePair<OperatorControllerOverload, List<int>>(overload, dist));
            }

            for (var j = 0; j < args.Count; j++)
            {
                if (distances.Count <= 1) break;
                var min = distances.Min(pair => pair.Value[j]);
                distances = distances.Where(pair => pair.Value[j] == min).ToList();
            }

            if (distances.Count == 0) ProcessOverloadErrors(false, overloads, args, funcName);
            if (distances.Count > 1) ProcessOverloadErrors(true, distances.Select(kv => kv.Key).ToList(), args, funcName);

            var t = distances[0].Key.OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            return Run(op, args, scope);
        }

        public static async Task<FieldControllerBase> Run(OperatorController op, List<FieldControllerBase> args, Scope scope = null)
        {
            var outDict = new Dictionary<KeyController, FieldControllerBase>();

            while (args.Count < op.Inputs.Count)
            {
                args.Add(null);
            }
            var inputs = new Dictionary<KeyController, FieldControllerBase>(args.Zip(op.Inputs, (arg, pair) => new KeyValuePair<KeyController, FieldControllerBase>(pair.Key, arg)));

            await op.Execute(inputs, outDict, null, scope);
            return outDict.Count == 0 ? null : outDict.First().Value;
        }

        private static void ProcessOverloadErrors(bool ambiguous, IEnumerable<OperatorControllerOverload> overloads, IReadOnlyCollection<FieldControllerBase> args, Op.Name funcName)
        {
            var properNumParams = false;
            var typeSublists = new List<KeyValuePair<int, string>>();
            var allParamCounts = new List<int>();

            foreach (var overload in overloads)
            {
                var typeInfoList = overload.ParamTypes.Select(kv => kv.Value.Type).ToList();
                var numParams = typeInfoList.Count;
                if (args.Count == numParams) properNumParams = true;
                if (!allParamCounts.Contains(numParams)) allParamCounts.Add(numParams);
                var operatorInfo = ambiguous ? $" -> {overload.OperatorType.ToString().Substring(5)}" : "";
                typeSublists.Add(new KeyValuePair<int, string>(numParams, $"({string.Join(", ", typeInfoList)})" + operatorInfo));
            }

            var oneElement = typeSublists.Count == 1;
            var sortedParams = typeSublists.OrderBy(x => x.Key).ToList();

            if (properNumParams)
            {
                var properTypes = sortedParams.Where(kv => kv.Key == args.Count).ToList();
                sortedParams.RemoveAll(kv => kv.Key == args.Count);
                if (!oneElement && !ambiguous) properTypes.Add(new KeyValuePair<int, string>(0, "^^"));
                properTypes.AddRange(sortedParams);
                sortedParams = properTypes;
            }
            else
            {
                var ordered = new List<KeyValuePair<int, string>>();
                var below = sortedParams.Where(kv => kv.Key < args.Count).ToList();
                var above = sortedParams.Where(kv => kv.Key > args.Count).ToList();
                ordered.AddRange(below);
                if (!oneElement && !ambiguous) ordered.Add(new KeyValuePair<int, string>(0, "--> ?"));
                ordered.AddRange(above);
                sortedParams = ordered;
            }
            var typesToString = sortedParams.Select(kv => kv.Value).ToList();

            throw new ScriptExecutionException(new OverloadErrorModel(ambiguous, funcName.ToString(), args.Select(ct => (ct != null) ? ct.TypeInfo : TypeInfo.None).ToList(), typesToString, allParamCounts));
        }

        public static ReferenceController CreateDocumentForOperator(IEnumerable<FieldControllerBase> arguments,
            Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;
            //TODO With overloading this isn't correct
            var t = _functionMap[funcName].First().OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            return CreateDocumentForOperator(arguments, op);
        }

        public static ReferenceController CreateDocumentForOperator(IEnumerable<FieldControllerBase> arguments, OperatorController op)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>(arguments.Zip(op.Inputs, (arg, pair) => new KeyValuePair<KeyController, FieldControllerBase>(pair.Key, arg)));
            var doc = new DocumentController(inputs, DocumentType.DefaultType);
            doc.SetField(KeyStore.OperatorKey, new ListController<OperatorController>(new[] { op }), true);

            return new DocumentReferenceController(doc, op.Outputs.FirstOrDefault().Key);

        }
    }
}
