using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
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

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var task = ParseSingleItem();
            task.Wait();
            stopwatch.Stop();
            return JsonDocument;
        }

        public static async Task ParseRecipes()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/RecipeReturn.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, "Assets/RecipeReturn.txt");
        }

        public static async Task ParseArrayOfObjects()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ArrayOfNestedDocumentJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, "Assets/RecipeReturn.txt");
        }

        public static async Task ParseYoutube()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/youtubeJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, "ms-appx:///Assets/youtubeJson.txt");
        }


        public static async Task ParseCustomer()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/customerJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            JsonDocument = Parse(jsonString, file.Path);
        }

        public static async Task ParseSingleItem()
        {
            var jsonString = @"[1,2,3]";
            JsonDocument = Parse(jsonString, "an/example/base/path");
        }

        public static DocumentController Parse(string json, string path)
        {
            var jtoken = JToken.Parse(json);
            var newSchema = new DocumentSchema(path);
            return ParseRoot(jtoken, newSchema);
        }

        private static DocumentController ParseRoot(JToken jtoken, DocumentSchema schema)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                var obj = ParseObject(jtoken, schema);
                return obj;
            }
            else
            {
                var key = schema.GetKey(jtoken);
                var field = jtoken.Type == JTokenType.Array ? ParseArray(jtoken, schema) : ParseValue(jtoken);
                SetDefaultFieldsOnPrototype(schema.Prototype, new Dictionary<Key, FieldModelController>{[key]=field});

                // wrap the field in an instance of the prototype
                var protoInstance = schema.Prototype.MakeDelegate();
                protoInstance.SetField(key, field, true);

                SetDefaultsOnActiveLayout(schema, protoInstance);
                return protoInstance;
            }
        }

        private static void SetDefaultsOnActiveLayout(DocumentSchema schema, DocumentController protoInstance)
        {
            var activeLayout = schema.Prototype.GetActiveLayout().Data.MakeDelegate();
            protoInstance.SetActiveLayout(activeLayout, true, false);
            var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            activeLayout.SetFields(defaultLayoutFields, true);
        }

        private static FieldModelController ParseChild(JToken jtoken, DocumentSchema parentSchema)
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
                return ParseArray(jtoken, parentSchema);
            }
            else
            {
                return ParseValue(jtoken);
            }
        }

        private static DocumentController ParseObject(JToken jtoken, DocumentSchema schema)
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
            SetDefaultsOnActiveLayout(schema, protoInstance);
            protoInstance.SetFields(fields, true);
            return protoInstance;
        }

        private static FieldModelController ParseArray(JToken jtoken, DocumentSchema schema)
        {
            var jArray = jtoken as JArray;

            if (jArray.Count == 0) return null; // if the array is empty we cannot know anything about it

            var fieldTypes = new HashSet<Type>(); // keep track of the number of field types we see
            var fieldList = new List<FieldModelController>(); // hold any fields we parse
            var docList = new List<DocumentController>(); // hold any documents we parse
            foreach (var item in jArray)
            {
                if (item.Type == JTokenType.Object) // if we have a document
                {
                    // create a schema for the document we just found if it is necessary
                    var childSchema = schema.AddChildSchemaOrReturnCurrentChild(item);
                    var parsedItem = ParseObject(item, childSchema);
                    docList.Add(parsedItem);
                } else if (item.Type == JTokenType.Array) // we fail on nested arrays
                {
                    return null;
                }
                else
                {
                    var fieldModelController = ParseValue(item);
                    fieldTypes.Add(fieldModelController.GetType());
                    fieldList.Add(fieldModelController);
                }
            }

            if (fieldList.Count == 0 && docList.Count != 0) // if we have documents but not fields
            {
                return new DocumentCollectionFieldModelController(docList); // return a document collection

            }
            if (fieldList.Count != 0 && docList.Count == 0 && fieldTypes.Count == 1) // if we have homogeneous fields but not documents
            {
                var fieldType = fieldTypes.FirstOrDefault();
                var genericListType = typeof(ListFieldModelController<>);
                var specificListType = genericListType.MakeGenericType(fieldType);
                var listController = Activator.CreateInstance(specificListType) as BaseListFieldModelController;
                listController.AddRange(fieldList);
                return listController; // return a new list
            }
            if (fieldList.Count != 0 && docList.Count == 0)
            {
                var listController = new ListFieldModelController<FieldModelController>();
                listController.AddRange(fieldList);
                return listController;
            }

            throw new NotImplementedException(" we don't support arrays of documents and values");
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
                        return ParseText(jtoken.ToObject<string>());
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

        private static FieldModelController ParseText(string text)
        {
            string[] _imageExtensions = { "jpg", "bmp", "gif", "png" }; //  etc
            foreach (var ext in _imageExtensions)
            {
                if (text.EndsWith(ext))
                {
                    return new ImageFieldModelController(new Uri(text));
                }
            }
            return new TextFieldModelController(text);
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

    internal class DocumentSchema 
    {
        public readonly string BasePath;

        private List<DocumentSchema> _schemas;

        public DocumentSchema(string basePath)
        {
            BasePath = basePath;
            Prototype = new DocumentController(new Dictionary<Key, FieldModelController>(), 
                new DocumentType(DashShared.Util.GetDeterministicGuid(BasePath), BasePath));
            SetDefaultLayoutOnPrototype(Prototype);
            _schemas = new List<DocumentSchema>();
        }

        public DocumentController Prototype { get; set; }

        public Key GetKey(JToken jToken)
        {
            var uniqueName = ConvertPathToUniqueName(BasePath + jToken.Path + jToken.Type);
            return new Key(DashShared.Util.GetDeterministicGuid(uniqueName))
            {
                Name = GetCleanNameFromJtokenPath(jToken.Path)
            };
        }

        private string GetCleanNameFromJtokenPath(string jTokenPath)
        {
            var splitOnPeriods = jTokenPath.Split('.');
            return splitOnPeriods.Last();
        }

        public DocumentSchema AddChildSchemaOrReturnCurrentChild(JToken jtoken)
        {
            var newPath = ConvertPathToUniqueName(BasePath + jtoken.Path);
            var currentSchema = _schemas.FirstOrDefault(s => s.BasePath == newPath);
            if (currentSchema == null)
            {
                currentSchema = new DocumentSchema(newPath);
                _schemas.Add(currentSchema);
            }
            return currentSchema;
        }

        public void SetDefaultLayoutOnPrototype(DocumentController prototype)
        {
            prototype.SetActiveLayout(new DefaultLayout().Document, true, true);
        }

        private string ConvertPathToUniqueName(string path)
        {
            return Regex.Replace(path, @"\[\d+?\]", "");
        }
    }

    
}
