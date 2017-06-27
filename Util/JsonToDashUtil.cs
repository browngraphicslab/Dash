using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dash
{
    public static class JsonToDashUtil
    {
        public static void RunTests()
        {
            Example();
        }

        public static async Task Example()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/youtubeJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            ParseJson(jtoken);
        }

        public static void ParseString()
        {
            var jsonString = @"{
                                  ""data"": {
                                    ""id"": ""42"",
                                    ""type"": ""people""
                                  }
                                    }";
            ParseJson(jsonString);
        }

        public static void ParseJson(JToken jToken)
        {

            // deal with object
            if (jToken.Type == JTokenType.Object)
            {
                var myObj = jToken as JObject;
                foreach (var sub_obj in myObj)
                {
                    ParseJson(sub_obj.Value);
                }
            }

            // deal with array
            else if (jToken.Type == JTokenType.Array)
            {
                var myArray = jToken as JArray;
                foreach (var item in myArray)
                {
                    ParseJson(item);
                }
            }

            // deal with value
            else
            {
                try
                {
                    var myValue = jToken as JValue;
                    var type = myValue.Type;
                    switch (type)
                    {
                        case JTokenType.None:
                            break;
                        case JTokenType.Object:
                            break;
                        case JTokenType.Array:
                            break;
                        case JTokenType.Constructor:
                            break;
                        case JTokenType.Property:
                            break;
                        case JTokenType.Comment:
                            break;
                        case JTokenType.Integer:
                            break;
                        case JTokenType.Float:
                            break;
                        case JTokenType.String:
                            break;
                        case JTokenType.Boolean:
                            break;
                        case JTokenType.Null:
                            break;
                        case JTokenType.Undefined:
                            break;
                        case JTokenType.Date:
                            break;
                        case JTokenType.Raw:
                            break;
                        case JTokenType.Bytes:
                            break;
                        case JTokenType.Guid:
                            break;
                        case JTokenType.Uri:
                            break;
                        case JTokenType.TimeSpan:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (InvalidCastException e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            
        }

        // recursively yield all children of json
        private static IEnumerable<JToken> AllChildren(JToken json)
        {
            foreach (var c in json.Children())
            {
                yield return c;
                foreach (var cc in AllChildren(c))
                {
                    yield return cc;
                }
            }
        }

    }
}
