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
        public static T CreateObject<T>(this string s) where T : ISerializable
        {
            var obj = JsonConvert.DeserializeObject<ISerializable>(s, _settings);
            Debug.Assert(obj is T);
            return (T)obj;
        }

        public static string Serialize(this ISerializable model)
        {
            return JsonConvert.SerializeObject(model, _settings);
        }
    }
}
