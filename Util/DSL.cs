using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// Public static API for using DSL (Dish Scripting Language)
    /// </summary>
    public static class DSL
    {
        /// <summary>
        /// Returns the string name for using the given operator as a Dish Function.
        /// Returns null if it doesn't have a declared name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operatorController"></param>
        /// <returns></returns>
        public static string GetFuncName<T>(T operatorController) where T : OperatorController
        {
            return operatorController.GetDishName();
        }

        /// <summary>
        /// Returns the string name for using the given operator type as a Dish Function.
        /// Returns null if it doesn't have a declared name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetFuncName<T>() where T : OperatorController
        {
            return OperatorScript.GetDishOperatorName<T>();
        }

        /// <summary>
        /// Method to call to execute a string as a Dish Script and return the FieldController return value.
        /// This method should throw exceptions if the string is not a valid script.
        /// If an InvalidDishScriptException is throw, the exception.ScriptErrorModel SHOULD be a helpful error message
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static FieldControllerBase Interpret(string script)
        {
            return OperatorScriptParser.Interpret(script);
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
            return OperatorScriptParser.GetScriptError(script);
        }


        /// <summary>
        /// Method to check if a given script is valid. 
        ///  This simply uses the GetScriptError method, which can called instead to get a helpful error message for invalid scripts.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsValidScript(string script)
        {
            return GetScriptError(script) != null;
        }

        public static string GetScriptForOperatorTree(OperatorController outputController)
        {
            //TODO not have this be 'Undefined' but rather just fail and tell user
            var controllerName = outputController.GetDishName() ?? "UNDEFINED";

            foreach (var input in outputController.Inputs)
            {
                
            }

            //TODO everything

            return null;
        }
    }
}
