using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dash
{
    public class JsonToDashUtil
    {
        static DocumentController JsonDocument = null;
        public static DocumentController RunTests()
        {
            //ParseYoutube();
            //var task = ParseCustomer();
            var task = RenderableJson();
            //var task = ParseArrayOfNestedDocument();
            task.Wait();
            return JsonDocument;
        }

        public static async Task ParseYoutube()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/youtubeJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var documentModel = ParseJson(jtoken, null, true);
            JsonDocument = ContentController.GetController(documentModel.Id) as DocumentController;
        }

        public static async Task ParseArrayOfNestedDocument()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ArrayOfNestedDocumentJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var documentModel = ParseJson(jtoken, null, true);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            JsonDocument = ContentController.GetController(documentModel.Id) as DocumentController;
        }

        public static async Task NestedArrayOfDocuments()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/nestedArraysOfDocumentsJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var documentModel = ParseJson(jtoken, null, true);
            JsonDocument = ContentController.GetController(documentModel.Id) as DocumentController;
        }

        public static async Task RenderableJson()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/renderableJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var documentModel = ParseJson(jtoken, null, true);
            JsonDocument = ContentController.GetController(documentModel.Id) as DocumentController;
        }

        public static async Task ParseCustomer()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/customerJson.txt"));
            var jsonString = await FileIO.ReadTextAsync(file);
            var jtoken = JToken.Parse(jsonString);
            var documentModel = ParseJson(jtoken, null, true);
            JsonDocument = ContentController.GetController(documentModel.Id) as DocumentController;
        }

        public static EntityBase ParseJson(JToken jToken, DocumentSchema parentSchema, bool isRoot, bool isChildOfArray = false)
        {
            DocumentSchema schema;

            // deal with object
            if (jToken.Type == JTokenType.Object)
            {
                var myObj = jToken as JObject;

                schema = isRoot ? new DocumentSchema(jToken.Path) : parentSchema.GetSchemaIfExistsElseCreateOne(jToken);

                var fields = new Dictionary<Key, FieldModel>();
                foreach (var subObj in myObj) // Parse the rest of the JSON recursively as fields of this document
                {
                    var key = schema.GetKeyIfExistsElseCreateOne(subObj.Key);
                    var fieldModel = ParseJson(subObj.Value, schema, false) as FieldModel;
                    fields[key] = fieldModel;
                }
                var documentType = new DocumentType(DashShared.Util.GenerateNewId(), "Root Document");
                var newDocumentRequestArgs = new CreateNewDocumentRequestArgs(fields, documentType);
                var newDocumentRequest = new CreateNewDocumentRequest(newDocumentRequestArgs);

                if (isRoot || isChildOfArray) // if we're the root object or the child of an array we should create a new document containing the rest of json as Fields
                {
                    return newDocumentRequest.GetReturnedDocumentModel();
                }

                // since we are not the root we should create a new document field model with the rest of json as fields
                return new DocumentModelFieldModel(newDocumentRequest.GetReturnedDocumentModel());
            }

            // deal with array
            if (jToken.Type == JTokenType.Array)
            {
                // forseeable issues here, 
                // 1. If we happen upon an array of values "text", "number", etc... we have no way of dealing with that
                // 2. If we happen upon an array of objects, then we need ParseJson to return those objects as DocumentModels,
                //      but ParseJson only returns the root as a DocumentModel. We can solve this with another parameter, but
                //      there might be a cleaner way.
                var myArray = jToken as JArray;
                var documentModels = new List<DocumentModel>();

                parentSchema = isRoot ? new DocumentSchema(jToken.Path) : parentSchema;

                foreach (var item in myArray)
                {
                    var dm = ParseJson(item, parentSchema, false, true) as DocumentModel;
                    if (dm == null)
                        throw new NotImplementedException("We have no way of creating lists of anything other than documents at the moment!");
                    documentModels.Add(dm);
                }
                var documentCollectionFieldModel = new DocumentCollectionFieldModel(documentModels);

                if (isRoot) // if the root is an array, we have to wrap it in a Document which can display the documentCollectionFieldModel as a field
                {
                    var documentType = new DocumentType(DashShared.Util.GenerateNewId(), "Root Document");
                    var fields = new Dictionary<Key, FieldModel>
                    {
                        [DashConstants.KeyStore.DataKey] = documentCollectionFieldModel
                    };
                    var newDocumentRequestArgs = new CreateNewDocumentRequestArgs(fields, documentType);
                    var newDocumentRequest = new CreateNewDocumentRequest(newDocumentRequestArgs);
                    return newDocumentRequest.GetReturnedDocumentModel();
                }
                // if the array is not the root we can just return the new DocumentCollectionFieldModel
                return documentCollectionFieldModel;
            }

            // deal with value
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

    public class DocumentSchema
    {
        public List<DocumentSchema> NestedDocumentSchemas { get; set; }

        public List<Key> DocumentKeys { get; set; }

        public DocumentType DocumentType { get; set; }

        public DocumentSchema(string path)
        {
            NestedDocumentSchemas = new List<DocumentSchema>();
            DocumentKeys = new List<Key>();
            DocumentType = new DocumentType(DashShared.Util.GenerateNewId(), ConvertPathToUniqueName(path));
        }

        private string ConvertPathToUniqueName(string path)
        {
            return Regex.Replace(path, @"\[\d+?\]", "");
        }

        public Key GetKeyIfExistsElseCreateOne(string keyName)
        {
            // try to get the current key from the list of document keys
            var currentKey = DocumentKeys.FirstOrDefault(key => key.Name == keyName);

            // if the current key is null then create one and add it to the list
            if (currentKey == null)
            {
                currentKey = new Key(DashShared.Util.GenerateNewId(), keyName);
                DocumentKeys.Add(currentKey);
            }

            // return the current key
            return currentKey;
        }

        public DocumentSchema GetSchemaIfExistsElseCreateOne(JToken jToken)
        {
            var uniquePath = ConvertPathToUniqueName(jToken.Path);
            var currentSchema = NestedDocumentSchemas.FirstOrDefault(schema => schema.DocumentType.Type == uniquePath);
            if (currentSchema == null)
            {
                Debug.Assert(ShouldWeGenerateSchema(jToken));
                currentSchema = new DocumentSchema(uniquePath);
                NestedDocumentSchemas.Add(currentSchema);
            }
            return currentSchema;
        }

        private static bool ShouldWeGenerateSchema(JToken jToken)
        {
            // either the path ends with [0] indicating that we are at the first document in an array
            // or the JToken is an object and we only have one instance of it so the path does not end
            // with brackets with a number inside i.e. [123]
            return jToken.Path.EndsWith("[0]") ||
                   (jToken.Type == JTokenType.Object && !Regex.IsMatch(jToken.Path, @"\[\d+\]$"));
        }
    }
}
