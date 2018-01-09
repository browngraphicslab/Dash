using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DashShared
{
    public static class SerializableExtensions
    {
        private static JsonSerializerSettings _settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// returns an object of type T from a string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static T CreateObject<T>(this string s) where T : ISerializable
        {
            var obj = JsonConvert.DeserializeObject<ISerializable>(s, _settings);
            Debug.Assert(obj is T);
            return (T)obj;
        }

        public static IEnumerable<T> CreateObjectList<T>(this string s) where T : class, ISerializable
        {
            try
            {
                var strings = JsonConvert.DeserializeObject<IEnumerable<string>>(s, _settings);
                //return strings;
                return strings.Select(str => str.CreateObject<T>());
                //Debug.Assert(obj.All(t => t is T));
                //return obj.Select(i => (T)i).ToList();
            }
            catch (Exception e)
            {
                return new List<T>();
                Debug.WriteLine("Failed to parse list of objects from strings to ienumerable!   "+e.Message);
            }
        }

        /// <summary>
        /// Returns a string of a serializable object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string Serialize(this ISerializable model)
        {
            return JsonConvert.SerializeObject(model, _settings);
        }


        public static T Clone<T>(this T source) where T: ISerializable
        {
            return CreateObject<T>(source.Serialize());
        }
    }
}
