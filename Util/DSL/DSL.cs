using System;
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
    // ReSharper disable once InconsistentNaming
    public class DSL
    {
        private readonly Scope _scope;
        private readonly DishReplView _replView;

        public DSL(Scope scope = null) => _scope = new Scope(scope);

        public DSL(OuterReplScope scope, DishReplView replView)
        {
            _scope = scope;
            _replView = replView;
        }

        public DSL(OuterReplScope scope)
        {
            _scope = scope;
        }

        public FieldControllerBase Run(string script, bool catchErrors =  false, bool undoVar = false)
        {
            try
            {
                if (CheckSpecialCommands(script)) return new TextController();

                var interpreted = TypescriptToOperatorParser.Interpret(script, _scope, undoVar);

                return interpreted;
            }
            catch (DSLException e)
            {
                if (!catchErrors) throw e;

                if (e is ScriptExecutionException doc) return doc.Error.GetErrorDoc(); 
                return new TextController(e.GetHelpfulString());
            }
        }

        private bool CheckSpecialCommands(string script)
        {
            // CLEAR
            if (script.Trim().Equals("clear") || script.Trim().Equals("clear all"))
            {
                _replView.Clear(script.Equals("clear all"));
                return true;
            }

            //CLOSE ALL
            if (script.Trim().Equals("close") || script.Trim().Equals("close all"))
            {
                _replView.Close(script.Equals("close all"));
                return true;
            }

            //TAB
            var indentSplit = script.Replace(" ", "").Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (indentSplit[0].Equals("tab"))
            {
                // DEFAULT
                if (indentSplit.Length == 1)
                {
                    _replView.SetIndent(3);
                    return true;
                }
                if (double.TryParse(indentSplit[1].Trim(), out double tab))
                {
                    _replView.SetIndent((int)tab);
                    return true;
                }
            }

            return false;
        }

        public FieldControllerBase GetOperatorController(string script, bool catchErrors = false)
        {
            try
            {
                var controller = TypescriptToOperatorParser.GetOperatorControllerForScript(script, _scope);
                return controller;
            }
            catch (DSLException e)
            {
                if (catchErrors)
                {
                    return new TextController(e.GetHelpfulString());
                }
                throw e;
            }
        }


        public FieldControllerBase this[string variableName]
        {
            get => _scope[variableName];
            set => _scope.SetVariable(variableName, value);
        }

        /// <summary>
        /// Returns the string name for using the given operator as a Dish Function.
        /// Returns null if it doesn't have a declared name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operatorController"></param>
        /// <returns></returns>
        public static Op.Name GetFuncName<T>(T operatorController) where T : OperatorController
        {
            return OperatorScript.GetDishOperatorName(operatorController);
        }

        /// <summary>
        /// Returns the string name for using the given operator type as a Dish Function.
        /// Returns null if it doesn't have a declared name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Op.Name GetFuncName<T>() where T : OperatorController
        {
            return OperatorScript.GetDishOperatorName<T>();
        }

        /// <summary>
        /// returns whether a certain function exists by its string name
        /// </summary>
        /// <param name="funcName"></param>
        /// <returns></returns>
        public static bool FuncNameExists(string funcName) => Op.TryParse(funcName, out var funcEnum) && OperatorScript.FuncNameExists(funcEnum);

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
                return TypescriptToOperatorParser.Interpret(script);
            }
            catch (DSLException e)
            {
                if (catchErrors)
                {
                    return new TextController(e.GetHelpfulString());
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
                return TypescriptToOperatorParser.GetOperatorControllerForScript(script);
            }
            catch (DSLException e)
            {
                if (catchErrors)
                {
                    return new TextController(e.GetHelpfulString());
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
        public static FieldControllerBase InterpretUserInput(string input, bool catchErrors = false, Scope scope = null)
        {
            var newInput = input?.Trim() ?? "";


            if (newInput.StartsWith("=="))
            {
                var dsl = new DSL(scope);
                return dsl.GetOperatorController(newInput.Remove(0, 2), catchErrors);//TODO we might need to prepend "return " to the input but maybe not?
            }

            if (newInput.StartsWith("="))
            {
                var dsl = new DSL(scope);
                return dsl.Run("return " + newInput.Remove(0, 1), catchErrors);
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
            return TypescriptToOperatorParser.GetScriptError(script);
        }


        /// <summary>
        /// Method to check if a given script is valid. 
        ///  This simply uses the GetScriptError method, which can called instead to get a helpful error message for invalid scripts.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        public static bool IsValidScript(string script)
        {
            return GetScriptError(script) == null;
        }

        /// <summary>
        /// Returns the string script of the given operator tree. 
        /// </summary>
        /// <param name="fieldController"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetScriptForOperatorTree(FieldControllerBase fieldController, Context context = null)
        {
            return (fieldController is TextController) ? 
                '"' + fieldController.GetValue(context).ToString() +'"' : 
                fieldController.GetValue(context).ToString();
        }
    }
}
