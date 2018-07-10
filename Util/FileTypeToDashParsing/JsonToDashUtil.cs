using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace Dash
{
    public class JsonToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(FileData fileData, DataPackageView dataView = null)
        {
            string jsonText;

            // if the uri filepath is a local file then copy it locally
            if (!fileData.File.FileType.EndsWith(".url"))
            {
                jsonText = await FileIO.ReadTextAsync(fileData.File);
            }
            // otherwise stream it from the internet
            else
            {
                // Get access to a HTTP ressource
                using (var stream = await fileData.FileUri.GetHttpStreamAsync())
                {
                    // Read the contents as ASCII text
                    jsonText = await stream.ReadTextAsync();
                }
            }

            // generate a unique json path if possible 
            var jsonPath = fileData.FileUri?.AbsolutePath ?? fileData.File.Path;
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                jsonPath = UtilShared.GenerateNewId();
            }

            return ParseJsonString(jsonText, jsonPath);
        }

        public DocumentController ParseJsonString(string json, string path)
        {
            if (!string.IsNullOrEmpty(json))
            {
                var jtoken = JToken.Parse(json);
                var newSchema = new DocumentSchema(path);
                return ParseRoot(jtoken, newSchema);
            }
            return null;
        }

        /// <summary>
        /// Parses the root of the Json object, this will recursively parse the rest of the json object
        /// </summary>
        /// <param name="jtoken"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        private DocumentController ParseRoot(JToken jtoken, DocumentSchema schema)
        {
            // if the root is an object parse the object
            if (jtoken.Type == JTokenType.Object)
            {
                var obj = ParseObject(jtoken, schema);
                return obj;
            }
            var key = schema.GetKey(jtoken);
            var field = jtoken.Type == JTokenType.Array ? ParseArray(jtoken, schema) : ParseValue(jtoken);
            SetDefaultFieldsOnPrototype(schema.Prototype,
                new Dictionary<KeyController, FieldControllerBase> { [key] = field });

            // wrap the field in an instance of the prototype
            var protoInstance = schema.Prototype.MakeDelegate();

            //DBTest.DBDoc.AddChild(protoInstance);
            protoInstance.SetField(key, field, true);

            SetDefaultsOnActiveLayout(schema, protoInstance);
            return protoInstance;
        }


        private void SetDefaultsOnActiveLayout(DocumentSchema schema, DocumentController protoInstance)
        {
            var activeLayout = schema.Prototype.GetActiveLayout().MakeDelegate();
            protoInstance.SetActiveLayout(activeLayout, true, false);
            var defaultLayoutFields = CourtesyDocument.DefaultLayoutFields(new Point(), new Size(200, 200));
            defaultLayoutFields.Add(KeyStore.CollectionViewTypeKey, new TextController(CollectionView.CollectionViewType.Schema.ToString()));
            activeLayout.SetFields(defaultLayoutFields, true);
        }

        private FieldControllerBase ParseChild(JToken jtoken, DocumentSchema parentSchema)
        {
            if (jtoken.Type != JTokenType.Object)
                return jtoken.Type == JTokenType.Array ? ParseArray(jtoken, parentSchema) : ParseValue(jtoken);
            // create a schema for the document we just found
            var childSchema = parentSchema.AddChildSchemaOrReturnCurrentChild(jtoken);
            var protoInstance = ParseObject(jtoken, childSchema);

            // wrap the document we found in a field model since it is not a root
            var docFieldModelController = protoInstance;
            return docFieldModelController;
        }

        private DocumentController ParseObject(JToken jtoken, DocumentSchema schema)
        {
            var jObject = jtoken as JObject;

            // parse each of the fields on the object into a field model controller
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            foreach (var jProperty in jObject)
            {
                var key = schema.GetKey(jProperty.Value);
                var fmc = ParseChild(jProperty.Value, schema);
                if (fmc == null) continue;
                fields[key] = fmc;
            }

            // update the prototype to contain default versions of all the parsed fields
            SetDefaultFieldsOnPrototype(schema.Prototype, fields);

            var protoInstance = schema.Prototype.MakeDelegate();
            //DBTest.DBDoc.AddChild(protoInstance);
            SetDefaultsOnActiveLayout(schema, protoInstance);
            protoInstance.SetFields(fields, true);
            return protoInstance;
        }

        private FieldControllerBase ParseArray(JToken jtoken, DocumentSchema schema)
        {
            var jArray = jtoken as JArray;

            if (jArray.Count == 0) return null; // if the array is empty we cannot know anything about it

            var fieldTypes = new HashSet<Type>(); // keep track of the number of field types we see
            var fieldList = new List<FieldControllerBase>(); // hold any fields we parse
            var docList = new List<DocumentController>(); // hold any documents we parse
            foreach (var item in jArray)
            {
                if (item.Type == JTokenType.Object) // if we have a document
                {
                    // create a schema for the document we just found if it is necessary
                    var childSchema = schema.AddChildSchemaOrReturnCurrentChild(item);
                    var parsedItem = ParseObject(item, childSchema);
                    docList.Add(parsedItem);
                }
                else if (item.Type == JTokenType.Array) // we fail on nested arrays
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
                return new ListController<DocumentController>(docList); // return a document collection

            }
            if (fieldList.Count != 0 && docList.Count == 0 && fieldTypes.Count == 1) // if we have homogeneous fields but not documents
            {
                var fieldType = fieldTypes.FirstOrDefault();
                var genericListType = typeof(ListController<>);
                var specificListType = genericListType.MakeGenericType(fieldType);
                var listController = Activator.CreateInstance(specificListType) as BaseListController;
                listController.AddRange(fieldList);
                return listController; // return a new list
            }
            if (fieldList.Count != 0 && docList.Count == 0)
            {
                var listController = new ListController<FieldControllerBase>();
                listController.AddRange(fieldList);
                return listController;
            }

            throw new NotImplementedException(" we don't support arrays of documents and values");
        }


        public FieldControllerBase ParseValue(JToken jtoken)
        {
            try
            {
                var myValue = jtoken as JValue;
                var type = myValue.Type;
                switch (type)
                {
                    case JTokenType.Null: // occurs on null fields
                        return new TextController("");
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        return new NumberController(jtoken.ToObject<double>());
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
                Debug.WriteLine(e);
                throw;
            }
        }

        private FieldControllerBase ParseText(string text)
        {
            string[] imageExtensions = { "jpg", "bmp", "gif", "png" }; //  etc
            if (imageExtensions.Any(text.EndsWith))
            {
                return new ImageController(new Uri(text));
            }
            return new TextController(text);
        }

        private void SetDefaultFieldsOnPrototype(DocumentController prototype, Dictionary<KeyController, FieldControllerBase> fields)
        {
            foreach (var field in fields)
            {
                if (prototype.GetField(field.Key) != null) continue;
                var defaultField = field.Value.GetDefaultController();
                prototype.SetField(field.Key, defaultField, true);
            }
        }
    }

    /// <summary>
    /// Essentially a utility class for maintaining a single prototype over a collection of documents that are parsed
    /// </summary>
    public class DocumentSchema
    {
        public readonly string BasePath;

        private List<DocumentSchema> _schemas;

        public DocumentSchema(string basePath)
        {
            BasePath = basePath;
            Prototype = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
                new DocumentType(UtilShared.GetDeterministicGuid(BasePath), BasePath));
            Prototype.SetField(KeyStore.AbstractInterfaceKey, new TextController(Prototype.DocumentType.Type + "API"), true);
            SetDefaultLayoutOnPrototype(Prototype);
            _schemas = new List<DocumentSchema>();
        }

        public DocumentController Prototype { get; set; }

        public KeyController GetKey(JToken jToken)
        {
            var uniqueName = ConvertPathToUniqueName(BasePath + jToken.Path + jToken.Type);
            // bcz: if jToken.Path is "", then it seems to cause problems later on because the key doesn't have a name (I think it gets filtered out of the KeyValue pane list)
            return new KeyController(GetCleanNameFromJtokenPath(jToken.Path == "" ? "JPATH" : jToken.Path), DashShared.UtilShared.GetDeterministicGuid(uniqueName));
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
