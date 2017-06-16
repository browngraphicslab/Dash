using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
{
    /// <summary>
    /// The result class is a wrapper for communicating with the server
    /// it has a content which is typed, a value for determining whether the result was succesful
    /// and a error message which can be displayed to the user
    /// </summary>
    /// <typeparam name="T">The content the result will return</typeparam>
    public class Result<T> where T : class 
    {
        /// <summary>
        /// Whether or not the result was succesful
        /// </summary>
        public readonly bool IsSuccess;

        /// <summary>
        /// Error message to display to the user if the result was not succesful
        /// </summary>
        public readonly string ErrorMessage;

        /// <summary>
        /// The content of the result
        /// </summary>
        public readonly T Content;

        /// <summary>
        /// Instantiates a new instance of the result class
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="errorMessage"></param>
        /// <param name="content"></param>
        public Result(bool isSuccess, string errorMessage="", T content=null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Content = content;
        }
    }

    public class Result : Result<object>
    {
        public Result(bool isSuccess, string errorMessage="") : base(isSuccess, errorMessage, null)
        {
        }
    }
}
