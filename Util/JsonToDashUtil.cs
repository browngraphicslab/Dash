using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Newtonsoft.Json.Linq;

namespace Dash
{
    public class JsonToDashUtil
    {
        static DocumentController JsonDocument = null;
        public static DocumentController RunTests()
        {

            var task = ParseSingleItem();
            task.Wait();
            return JsonDocument;
        }

        public static async Task ParseRecipes()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/RecipeReturn.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, "Assets/RecipeReturn.txt");
        }


        public static async Task ParseCustomer()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/customerJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, file.Path);
        }

        public static async Task ParseSingleItem()
        {
            var jsonString = @"""A single piece of text""";
            JsonDocument = Parse(jsonString, "an/example/base/path");
        }

        private static DocumentController Parse(string json, string path)
        {
            var jtoken = JToken.Parse(json);
            var newSchema = new NewDocumentSchema(path);
            return ParseRoot(jtoken, newSchema);
        }

        private static DocumentController ParseRoot(JToken jtoken, NewDocumentSchema schema)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                var obj = ParseObject(jtoken, schema);
                return obj;
            }
            else if (jtoken.Type == JTokenType.Array)
            {
                throw new NotImplementedException();
            }
            else
            {
                var key = schema.GetKey(jtoken);
                var field = ParseValue(jtoken);
                SetDefaultFieldsOnPrototype(schema.Prototype, new Dictionary<Key, FieldModelController>{[key]=field});

                // wrap the field in an instance of the prototype
                var protoInstance = schema.Prototype.MakeDelegate();
                protoInstance.SetField(key, field, true);
                return protoInstance;
            }
        }

        private static FieldModelController ParseChild(JToken jtoken, NewDocumentSchema parentSchema)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                // create a schema for the document we just found
                var childSchema = parentSchema.AddChildSchemaOrReturnCurrentChild(jtoken);
                var protoInstance = ParseObject(jtoken, childSchema);

                // wrap the document we found in a field model since it is not a root
                var docFieldModelController = new DocumentFieldModelController(protoInstance);
                return docFieldModelController;
            } else if (jtoken.Type == JTokenType.Array)
            {
                throw new NotImplementedException();
            }
            else
            {
                return ParseValue(jtoken);
            }
        }

        private static DocumentController ParseObject(JToken jtoken, NewDocumentSchema schema)
        {
            var jObject = jtoken as JObject;

            // parse each of the fields on the object into a field model controller
            var fields = new Dictionary<Key, FieldModelController>();
            foreach (var jProperty in jObject)
            {
                var key = schema.GetKey(jProperty.Value);
                var fmc = ParseChild(jProperty.Value, schema);
                fields[key] = fmc;
            }

            // update the prototype to contain default versions of all the parsed fields
            SetDefaultFieldsOnPrototype(schema.Prototype, fields);

            var protoInstance = schema.Prototype.MakeDelegate();
            protoInstance.SetFields(fields, true);
            return protoInstance;
        }

        private static FieldModelController ParseValue(JToken jtoken)
        {
            try
            {
                var myValue = jtoken as JValue;
                var type = myValue.Type;
                switch (type)
                {
                    case JTokenType.Null: // occurs on null fields
                        return new TextFieldModelController("");
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        return new NumberFieldModelController(jtoken.ToObject<double>());
                    case JTokenType.String:
                    case JTokenType.Boolean:
                    case JTokenType.Date:
                    case JTokenType.Uri:
                    case JTokenType.Guid:
                    case JTokenType.TimeSpan:
                        return new TextFieldModelController(jtoken.ToObject<string>());
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void SetDefaultFieldsOnPrototype(DocumentController prototype, Dictionary<Key, FieldModelController> fields)
        {
            foreach (var field in fields)
            {
                if (prototype.GetField(field.Key) == null)
                {
                    var defaultField = field.Value.GetDefaultController();
                    prototype.SetField(field.Key, defaultField, true);
                }
            }
        }

       
    }

    internal class NewDocumentSchema 
    {
        public readonly string BasePath;

        private List<NewDocumentSchema> _schemas;

        public NewDocumentSchema(string basePath)
        {
            BasePath = basePath;
            Prototype = new DocumentController(new Dictionary<Key, FieldModelController>(), 
                new DocumentType(DashShared.Util.GetDeterministicGuid(BasePath), BasePath));
            SetDefaultLayoutOnPrototype(Prototype);
            _schemas = new List<NewDocumentSchema>();
        }

        public DocumentController Prototype { get; set; }

        public Key GetKey(JToken jToken)
        {
            return new Key(DashShared.Util.GetDeterministicGuid(BasePath + jToken.Path + jToken.Type))
            {
                Name = BasePath + jToken.Path
            };
        }

        public NewDocumentSchema AddChildSchemaOrReturnCurrentChild(JToken jtoken)
        {
            var newPath = BasePath + jtoken.Path;
            var currentSchema = _schemas.FirstOrDefault(s => s.BasePath == newPath);
            if (currentSchema == null)
            {
                currentSchema = new NewDocumentSchema(newPath);
                _schemas.Add(currentSchema);
            }
            return currentSchema;
        }

        public void SetDefaultLayoutOnPrototype(DocumentController prototype)
        {
            var _ = new LayoutCourtesyDocument(prototype);
        }
    }

    
}
