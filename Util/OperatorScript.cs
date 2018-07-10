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
        private static Dictionary<string, Type> _functionMap;
        private static Dictionary<Type, string> _reverseFunctionMap;

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

            _functionMap = new Dictionary<string, Type>();
            _reverseFunctionMap = new Dictionary<Type, string>();
            foreach (var operatorType in GetTypesWithOperatorAttribute(Assembly.GetExecutingAssembly()))
            {
                //IF YOU CRASHED ON THIS LINE THEN YOU PROBABLY ADDED A NEW OPERATOR WITHOUT AN EMPTY CONSTRUCTOR. 
                OperatorController op = (OperatorController)Activator.CreateInstance(operatorType);

                var typeNames = operatorType.GetCustomAttribute<OperatorTypeAttribute>().GetTypeNames();

                foreach (var typeName in typeNames)
                {
                    _functionMap[typeName] = operatorType;
                    _reverseFunctionMap[operatorType] = typeName;


                    if (PrintAllFuncDocumentation)
                    {
                        PrintDocumentation(typeName, op);
                    }
                }

            }

            if (PrintAllFuncDocumentation)
            {
                Debug.WriteLine("\n\n\n\n\n");
            }
        }

        private static void PrintDocumentation(string funcName, OperatorController op)
        {
            var doc = op.Outputs[0].Value.ToString()+"   "+funcName + "( " + string.Join(',', op.Inputs.Select(i => " "+i.Value.Type.ToString() + "  "+  i.Key.Name.ToLower())) + " );";
            FunctionDocumentation += doc + "         \n";
            Debug.WriteLine(doc);
        }

        /// <summary>
        /// gets all the keys of a function's parameters but as a dictionary of key name to key controller
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static Dictionary<string, KeyController> GetKeyControllersForFunction(string funcName)
        {
            return GetOrderedKeyControllersForFunction(funcName).ToDictionary(k => k.Name, v => v);
        }

        /// <summary>
        /// returns the dish function name for a given operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetDishOperatorName<T>() where T : OperatorController
        {
            var t = typeof(T);

            //if this fails then the function name doens't exist for the given controller
            Debug.Assert(_reverseFunctionMap.ContainsKey(t));

            return _reverseFunctionMap.ContainsKey(t) ? _reverseFunctionMap[t] : null;
        }

        private static OperatorController GetOperatorWithName(string funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController) Activator.CreateInstance(t);
                return op;
            }

            return null;
        }

        /// <summary>
        /// returns the dish function name for a given operator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetDishOperatorName<T>(T controller) where T : OperatorController
        {
            //if this fails then the function name doens't exist for the given controller

            return controller.GetType().GetCustomAttribute<OperatorTypeAttribute>().GetTypeNames().First();
        }


        /// <summary>
        /// returns whether a certain function exists based on a string name
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static bool FuncNameExists(string funcName)
        {
            return _functionMap.ContainsKey(funcName);
        }

        public static DashShared.TypeInfo GetOutputType(string funcName)
        {
            return GetOperatorWithName(funcName)?.Outputs?.ElementAt(0).Value ?? DashShared.TypeInfo.None;
        }

        public static DashShared.TypeInfo GetFirstInputType(string funcName)
        {
            return GetOperatorWithName(funcName)?.Inputs?.ElementAt(0).Value.Type ?? DashShared.TypeInfo.None;
        }

        /// <summary>
        /// returns an ordered list of the keycontorllers in a function
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static List<KeyController> GetOrderedKeyControllersForFunction(string funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController)Activator.CreateInstance(t);
                return op.Inputs.ToList().Select(i => i.Key).ToList();
            }
            return null;
        }


        public static Dictionary<KeyController, IOInfo> GetKeyControllerDictionaryForFunction(string funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController)Activator.CreateInstance(t);
                return op.Inputs.ToDictionary(k => k.Key, v => v.Value);
            }
            return null;
        }

        private static IEnumerable<Type> GetTypesWithOperatorAttribute(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(OperatorTypeAttribute), true).Length > 0)
                {
                    Activator.CreateInstance(type);
                    yield return type;
                }
            }
        }

        public static FieldControllerBase Run(string funcName, Dictionary<KeyController, FieldControllerBase> args, ScriptState state = null)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController) Activator.CreateInstance(t);
                var outDict = new Dictionary<KeyController, FieldControllerBase>();
                op.Execute(args,outDict, null, state);
                if (outDict.Count == 0)
                {
                    return null;
                }
                return outDict.First().Value;
            }
            return null;
        }


        public static ReferenceController CreateDocumentForOperator(IEnumerable<KeyValuePair<KeyController, FieldControllerBase>> parameters, string funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController) Activator.CreateInstance(t);

                var doc = new DocumentController();

                foreach (var parameter in parameters)
                {
                    doc.SetField(parameter.Key, parameter.Value, true);
                }
                doc.SetField(KeyStore.OperatorKey, new ListController<OperatorController>(new OperatorController[] { op }), true);

                return new DocumentReferenceController(doc, op.Outputs.FirstOrDefault().Key);
                
            }

            return null;
        }
    }
}
