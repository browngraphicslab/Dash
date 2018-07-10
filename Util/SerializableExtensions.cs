using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    public static class SerializableExtensions
    {
        /// <summary>
        /// This method converts any serializable to a document controller equivalent to the model you passed in.  
        /// This might help us bridge the gap between how Dash works under the hood and what the user can control.
        /// I also just wanted to see if this would be possible
        /// </summary>
        /// <param name="serializable"></param>
        /// <returns></returns>
        public static DocumentController ConvertToDocument(this ISerializable serializable)
        {
            var stringified = serializable.Serialize();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringified);
            var controller = new DocumentController();

            var t = serializable.GetType();
            var typeIdentifierId = t.FullName.ToLower() + "_" + t.GUID;

            foreach (var kvp in dict)
            {
                var newKey = new KeyController(kvp.Key, kvp.Key + "__from__" + typeIdentifierId);
                FieldControllerBase newField;
                try
                {
                    newField = DSL.Interpret(kvp.Value.ToString(), false);
                }
                catch (DSLException e)
                {
                    newField = new TextController(kvp.Value.ToString());
                }

                controller.SetField(newKey, newField, true);
            }

            return controller;
        }
    }
}
