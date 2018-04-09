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

        private OperatorScript()
        {
            Init();
        }

        private void Init()
        {
            _functionMap = new Dictionary<string, Type>();
            _reverseFunctionMap = new Dictionary<Type, string>();
            foreach (var operatorType in GetTypesWithOperatorAttribute(Assembly.GetExecutingAssembly()))
            {
                //IF YOU CRASHED ON THIS LINE THEN YOU PROBABLY ADDED A NEW OPERATOR WITHOUT AN EMPTY CONSTRUCTOR. 
                OperatorController op = (OperatorController)Activator.CreateInstance(operatorType);

                var typeName = operatorType.GetCustomAttribute<OperatorTypeAttribute>().GetType();

                _functionMap[typeName] = operatorType;
                _reverseFunctionMap[operatorType] = typeName;
            }
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
            return GetOperatorWithName(funcName)?.Outputs.ElementAt(0).Value ?? DashShared.TypeInfo.None;
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

        public static FieldControllerBase Run(string funcName, Dictionary<KeyController, FieldControllerBase> args)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController) Activator.CreateInstance(t);
                var outDict = new Dictionary<KeyController, FieldControllerBase>();
                op.Execute(args,outDict, null);
                return outDict.First().Value;
            }
            return null;
        }
    }
}
