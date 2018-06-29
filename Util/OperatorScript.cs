using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

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
            PrintAllFuncDocumentation = true;
            Init();
        }

        private void Init()
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
                    } else
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

            DishReplView.SetDataset(_functionMap.Keys.Select(k => k.ToString()).ToList());

            if (PrintAllFuncDocumentation)
            {
                Debug.WriteLine("\n\n\n\n\n");
            }
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

        private static OperatorController GetOperatorWithName(Op.Name funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                //TODO With overloading this isn't correct
                var t = _functionMap[funcName].First().OperatorType;
                var op = (OperatorController)Activator.CreateInstance(t);
                return op;
            }

            return null;
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
        public static bool FuncNameExists(Op.Name funcName)
        {
            return _functionMap.ContainsKey(funcName);
        }

        //TODO With overloads we need more info to have this make sense
        public static DashShared.TypeInfo GetOutputType(Op.Name funcName)
        {
            return GetOperatorWithName(funcName)?.Outputs?.ElementAt(0).Value ?? DashShared.TypeInfo.None;
        }

        //TODO With overloads we need more info to have this make sense
        public static DashShared.TypeInfo GetFirstInputType(Op.Name funcName)
        {
            return GetOperatorWithName(funcName)?.Inputs?.ElementAt(0).Value.Type ?? DashShared.TypeInfo.None;
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

        public static FieldControllerBase Run(Op.Name funcName, List<FieldControllerBase> args, Scope scope = null)
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

            if (distances.Count == 0)
            {
                //No valid overloads
                //List<>
                throw new ScriptExecutionException(new OverloadErrorModel(false, funcName.ToString()));
            }

            if (distances.Count > 1)
            {
                //Ambiguous overloads
                throw new ScriptExecutionException(new OverloadErrorModel(true, funcName.ToString()));
            }

            var t = distances[0].Key.OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);
            var outDict = new Dictionary<KeyController, FieldControllerBase>();

            var inputs = new Dictionary<KeyController, FieldControllerBase>(args.Zip(op.Inputs, (arg, pair) => new KeyValuePair<KeyController, FieldControllerBase>(pair.Key, arg)));

            op.Execute(inputs, outDict, null, scope);
            return outDict.Count == 0 ? null : outDict.First().Value;
        }

        public static ReferenceController CreateDocumentForOperator(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> parameters, Op.Name funcName)
        {
            if (!_functionMap.ContainsKey(funcName)) return null;
            //TODO With overloading this isn't correct
            var t = _functionMap[funcName].First().OperatorType;
            var op = (OperatorController)Activator.CreateInstance(t);

            var doc = new DocumentController();

            foreach (var parameter in parameters)
            {
                doc.SetField(parameter.Key, parameter.Value, true);
            }
            doc.SetField(KeyStore.OperatorKey, new ListController<OperatorController>(new OperatorController[] { op }), true);

            return new DocumentReferenceController(doc.Id, op.Outputs.FirstOrDefault().Key);

        }
    }
}
