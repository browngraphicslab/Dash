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
            //ParseYoutube();
            ParseCustomer();
        }

        public static async Task ParseYoutube()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/youtubeJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            ParseJson(jtoken, true);
        }

        public static async Task ParseCustomer()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/customerJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var documentModel = ParseJson(jtoken, true);
            var documentController = ContentController.GetController(documentModel.Id);
        }

        public static void ParseString()
        {
            var jsonString = @"{
                                  ""data"": {
                                    ""id"": ""42"",
                                    ""type"": ""people""
                                  }
                                    }";
            ParseJson(jsonString, true);

        }

        public static EntityBase ParseJson(JToken jToken, bool isRoot, bool parentIsArray = false)
        {

            // deal with object
            if (jToken.Type == JTokenType.Object)
            {
                var myObj = jToken as JObject;

                var fields = new Dictionary<Key, FieldModel>();
                foreach (var sub_obj in myObj) // Parse the rest of the JSON recursively as fields of this document
                {
                    var key = new Key(DashShared.Util.GenerateNewId(), sub_obj.Key);
                    var fieldModel = ParseJson(sub_obj.Value, false) as FieldModel;
                    fields[key] = fieldModel;
                }
                var documentType = new DocumentType(DashShared.Util.GenerateNewId(), "Root Document");
                var newDocumentRequestArgs = new CreateNewDocumentRequestArgs(fields, documentType);
                var newDocumentRequest = new CreateNewDocumentRequest(newDocumentRequestArgs);

                if (isRoot) // if we're the root object we should create a new document containing the rest of json as Fields
                {
                    return newDocumentRequest.GetReturnedDocumentModel();
                }

                // since we are not the root we should create a new document field model with the rest of json as fields
                return new DocumentModelFieldModel(newDocumentRequest.GetReturnedDocumentModel());
            }

            // deal with array
            else if (jToken.Type == JTokenType.Array)
            {
                throw new NotImplementedException();
                // forseeable issues here, 
                // 1. If we happen upon an array of values "text", "number", etc... we have no way of dealing with that
                // 2. If we happen upon an array of objects, then we need ParseJson to return those objects as DocumentModels,
                //      but ParseJson only returns the root as a DocumentModel. We can solve this with another parameter, but
                //      there might be a cleaner way.
                var myArray = jToken as JArray;
                foreach (var item in myArray)
                {
                    ParseJson(item, false);
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
                        case JTokenType.Object: // A Json Object is defined by {}
                        case JTokenType.Array: // A Json Array is defined by []
                        case JTokenType.Property: // A Json Property is a (Key, JToken) pair and can only be found in Json Objects
                            throw new NotImplementedException("We should have dealt with this earlier");
                        case JTokenType.Constructor:
                        case JTokenType.Comment:
                        case JTokenType.Null:
                        case JTokenType.Raw:
                        case JTokenType.Undefined:
                        case JTokenType.Bytes:
                        case JTokenType.None:
                            throw new NotImplementedException();
                        case JTokenType.Integer:
                        case JTokenType.Float:
                            return new NumberFieldModel(jToken.ToObject<double>());
                        case JTokenType.String:
                        case JTokenType.Boolean:
                        case JTokenType.Date:
                        case JTokenType.Uri:
                        case JTokenType.Guid:
                        case JTokenType.TimeSpan:
                            return new TextFieldModel(jToken.ToObject<string>());
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
