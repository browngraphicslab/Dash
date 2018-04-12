﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    /// <summary>
    /// Class used to execute DSL (Dish Scripting Language).  
    /// This class can be instantiated to use local state, 
    /// or can be used as a Public static API for using DSL
    /// </summary>
    public class DSL
    {
        private ScriptState _state;
        public DSL(ScriptState state = null)
        {
            _state = state ?? new ScriptState();
        }

        /// <summary>
        /// Returns the string name for using the given operator as a Dish Function.
        /// Returns null if it doesn't have a declared name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operatorController"></param>
        /// <returns></returns>
        public static string GetFuncName<T>(T operatorController) where T : OperatorController
        {
            return OperatorScript.GetDishOperatorName(operatorController);
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
        /// returns whether a certain function exists by its string name
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static bool FuncNameExists(string funcName)
        {
            return OperatorScript.FuncNameExists(funcName);
        }

        /// <summary>
        /// Method to call to execute a string as a Dish Script and return the FieldController return value.
        /// This method should throw exceptions if the string is not a valid script.
        /// If an InvalidDishScriptException is throw, the exception.ScriptErrorModel SHOULD be a helpful error message.
        /// 
        /// if catchErrors is true, you will get all errors back as a helpful string wrapped in a textController
        /// </summary>
        /// <param name="script"></param>
        /// <param name="catchErrors"></param>
        /// <returns></returns>
        public static FieldControllerBase Interpret(string script, bool catchErrors = false)
        {
            try
            {
                return OperatorScriptParser.Interpret(script);
            }
            catch (InvalidDishScriptException e)
            {
                if (catchErrors)
                {
                    return new TextController(e.ScriptErrorModel.GetHelpfulString());
                }
                throw e;
            }
            catch (DSLException e)
            {
                if (catchErrors)
                {
                    return new TextController("Execution Error: " + e.Message);
                }
                throw e;
            }
        }


        /// <summary>
        /// Method to call to get an operator controller that represents the script called
        /// 
        /// if catchErrors is true, you will get all errors back as a helpful string wrapped in a textController
        /// </summary>
        /// <param name="script"></param>
        /// <param name="catchErrors"></param>
        /// <returns></returns>
        public static FieldControllerBase GetOperatorControllerForScript(string script, bool catchErrors = false)
        {
            try
            {
                return OperatorScriptParser.GetOperatorControllerForScript(script);
            }
            catch (InvalidDishScriptException e)
            {
                if (catchErrors)
                {
                    return new TextController(e.ScriptErrorModel.GetHelpfulString());
                }
                throw e;
            }
            catch (DSLException e)
            {
                if (catchErrors)
                {
                    return new TextController("Execution Error: "+e.Message);
                }
                throw e;
            }
        }


        /// <summary>
        /// Method to call to parse a user's intention when entering a field.  
        /// THis will evaluate correctly with '=' and '=='  and return the appropriate references or literals      
        /// 
        /// if catchErrors is true, you will get all errors back as a helpful string wrapped in a textController
        /// </summary>
        /// <param name="input"></param>
        /// <param name="catchErrors"></param>
        /// <returns></returns>
        public static FieldControllerBase InterpretUserInput(string input, bool catchErrors = false)
        {
            var newInput = input?.Trim() ?? "";


            if (newInput.StartsWith("=="))
            {
                return GetOperatorControllerForScript(newInput.Remove(0, 2), catchErrors);
            }

            if (newInput.StartsWith("="))
            {
                return Interpret(newInput.Remove(0, 1), catchErrors);
            }


            return new TextController(newInput);
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

        /// <summary>
        /// Returns the string script of the given operator tree. 
        /// </summary>
        /// <param name="fieldController"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetScriptForOperatorTree(FieldControllerBase fieldController, Context context = null)
        {
            return fieldController.GetValue(context).ToString();
        }
    }
}
