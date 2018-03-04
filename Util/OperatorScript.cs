using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class OperatorScript
    {
        public static OperatorScript Instance = new OperatorScript();
        private static Dictionary<string, Type> _functionMap;

        private OperatorScript()
        {
            Init();
        }

        private void Init()
        {
            _functionMap = new Dictionary<string, Type>();
            foreach (var operatorType in GetTypesWithOperatorAttribute(Assembly.GetExecutingAssembly()))
            {
                //IF YOU CRASHED ON THIS LINE THEN YOU PROBABLY ADDED A NEW OPERATOR WITHOUT AN EMPTY CONSTRUCTOR. 
                //OperatorController op = (OperatorController)Activator.CreateInstance(operatorType);
                _functionMap[operatorType.GetCustomAttribute<OperatorTypeAttribute>().GetType()] = operatorType;
            }
        }

        public static Dictionary<string, KeyController> GetKeyControllersForFunction(string funcName)
        {
            if (_functionMap.ContainsKey(funcName))
            {
                var t = _functionMap[funcName];
                var op = (OperatorController)Activator.CreateInstance(t);
                return op.Inputs.Keys.ToDictionary(k => k.Name, v => v);
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
