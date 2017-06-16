using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Dash
{
    /// <summary>
    /// Wrapper Class to deal with excpetions thrown by requests to the api
    /// </summary>
    public sealed class ApiException : Exception
    {
        /// <summary>
        /// The actual response from the api
        /// </summary>
        private readonly HttpResponseMessage _response;

        /// <summary>
        /// The StatusCode of the response
        /// </summary>
        private HttpStatusCode StatusCode => _response.StatusCode;

        private HttpMethod Method => _response.RequestMessage.Method;

        private Uri RequestUri => _response.RequestMessage.RequestUri;

        /// <summary>
        /// The List of Errors, essentially wraps a dictionary provided by the Exception class
        /// </summary>
        public List<string> Errors => Data.Values.Cast<string>().ToList();

        /// <summary>
        /// Constructor to create a new instance of this wrapper class
        /// </summary>
        /// <param name="response"></param>
        public ApiException(HttpResponseMessage response)
        {
            _response = response;

            var httpErrorObject = response.Content.ReadAsStringAsync().Result;

            // Create an anonymous object to use as the template for deserialization:
            var anonymousErrorObject =
                new { message = "", ModelState = new Dictionary<string, string[]>() };

            // Deserialize:
            var deserializedErrorObject =
                JsonConvert.DeserializeAnonymousType(httpErrorObject, anonymousErrorObject);

            // Check to see if the ModelState has any errors included in it
            if (deserializedErrorObject.ModelState != null)
            {
                // join the model state error labels with their reasons using a period and space
                var errors = deserializedErrorObject.ModelState.Select(kvp => string.Join(". ", kvp.Value)).ToArray();

                // add all the errors to the Data dictionary
                for (var i = 0; i < errors.Length; i++)
                {
                    // Wrap the errors up into the base Exception.Data Dictionary:
                    Data.Add(i, errors.ElementAt(i));
                }
            }
            // Othertimes, there may not be Model Errors:
            else
            {
                var error =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(httpErrorObject);
                foreach (var kvp in error)
                {
                    // Wrap the errors up into the base Exception.Data Dictionary:
                    Data.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public string ApiExceptionMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  An Error Occurred:");
            sb.AppendLine($"  Status Code: {StatusCode}");
            sb.AppendLine($"  Request Uri: {RequestUri}");
            sb.AppendLine($"  Method: {Method}");
            sb.AppendLine("  Errors:");
            foreach (var error in Errors)
            {
                sb.AppendLine($"    {error}");
            }
            return sb.ToString();
        }
    }
}
