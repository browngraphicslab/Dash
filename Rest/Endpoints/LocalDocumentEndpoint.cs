using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dash
{
    public class LocalDocumentEndpoint : IDocumentEndpoint
    {
        /// <summary>
        /// private dictionary here to save your objects in memory.  Should be synced with the local dash files
        /// </summary>
        private Dictionary<string, string> _modelDictionary;

        /// <summary>
        /// private timer that simple calls a callback every time interval and forces this class to save the current objects
        /// </summary>
        private Timer _saveTimer;
        private IKeyEndpoint _keys => App.Instance.Container.GetRequiredService<IKeyEndpoint>();

        private IFieldEndpoint _fields => App.Instance.Container.GetRequiredService<IFieldEndpoint>();

        public LocalDocumentEndpoint()
        {
            _saveTimer = new Timer(SaveTimerCallback, null, new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond),  new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond));
            try
            {
                var dictionaryText = File.ReadAllText(DashConstants.LocalStorageFolder.Path + "\\"+ DashConstants.LocalServerDocumentFilepath);
                _modelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryText);
                _modelDictionary = _modelDictionary ?? new Dictionary<string, string>();
            }
            catch (Exception e)
            {
                _modelDictionary = new Dictionary<string, string>();
            }
            App.Instance.Suspending += AppSuspending;
        }

        /// <summary>
        /// Event handler called every tme interval that saves the current version of your objects
        /// </summary>
        /// <param name="state"></param>
        private async void SaveTimerCallback(object state)
        {
            var file =
                await DashConstants.LocalStorageFolder.CreateFileAsync(DashConstants.LocalServerDocumentFilepath,CreationCollisionOption.OpenIfExists);
            using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                using (var outgoingStream = stream.GetOutputStreamAt(0))
                {
                    using (var dw = new DataWriter(outgoingStream))
                    {
                        dw.WriteString(JsonConvert.SerializeObject(_modelDictionary));
                        await dw.StoreAsync();
                        await dw.FlushAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Private event handler called whenever the appo is suspending or closing, just saves a final time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppSuspending(object sender, SuspendingEventArgs e)
        {
            SaveTimerCallback(null);
        }


        public async Task AddDocument(DocumentModel newDocument, Action<DocumentModel> success, Action<Exception> error)
        {
            _modelDictionary[newDocument.Id] = JsonConvert.SerializeObject(newDocument);
            success(newDocument);
        }

        public void UpdateDocument(DocumentModel documentToUpdate, Action<DocumentModel> success, Action<Exception> error)
        {
            _modelDictionary[documentToUpdate.Id] = JsonConvert.SerializeObject(documentToUpdate);
            success(documentToUpdate);
        }

        public async Task GetDocument(string id, Func<DocumentModelDTO, Task> success, Action<Exception> error)
        {
            try
            {
                var doc = _modelDictionary[id];
                await success(JsonConvert.DeserializeObject<DocumentModelDTO>(doc));
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public async Task GetDocuments(IEnumerable<string> ids, Func<IEnumerable<DocumentModelDTO>, Task> success, Action<Exception> error)
        {
            try
            {
                var list = new List<DocumentModel>();
                foreach (var id in ids)
                {
                    var text = _modelDictionary[id];
                    var doc = JsonConvert.DeserializeObject<DocumentModel>(text);
                    list.Add(doc);
                }
                Debug.WriteLine("GET DOCS CALLED");
                await ToDtoAsync(list, success);
                Debug.WriteLine("GET DOCS RETURNED");
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        //why is this delete paramterized with a document model? dont ask me
        public void DeleteDocument(DocumentModel document, Action success, Action<Exception> error)
        {
            try
            {
                _modelDictionary.Remove(document.Id);
                success();
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            _modelDictionary = new Dictionary<string, string>();
            success();
        }

        public async Task GetDocumentByType(DocumentType documentType, Func<IEnumerable<DocumentModelDTO>, Task> success, Action<Exception> error)
        {
            try
            {
                var actualModels = _modelDictionary.Select(v => JsonConvert.DeserializeObject<DocumentModel>(v.Value));
                var array = actualModels.Where(i => i.DocumentType.Equals( documentType)).ToArray();
                Debug.WriteLine("GET DOCS BY TYPE CALLED");

                await ToDtoAsync(array,success);
                Debug.WriteLine("GET DOCS BY TYPE RETURNED");
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        private async Task<IEnumerable<DocumentModelDTO>> CreateModels(IEnumerable<DocumentModel> models, Dictionary<string, FieldModelDTO> fields, Dictionary<string, KeyModel> keys, Func<IEnumerable<DocumentModelDTO>,Task> success )
        {
            var list = new List<DocumentModelDTO>();
            foreach (var model in models)
            {
                var fieldObjs = model.Fields.Values.Select(fieldId => fields[fieldId]).ToArray();
                var keyObjs = model.Fields.Keys.Select(keyId => keys[keyId]).ToArray();
                list.Add(new DocumentModelDTO(fieldObjs,keyObjs, model.DocumentType, model.Id));
            }
            await success?.Invoke(list);
            return list;
        }

        private async Task ToDtoAsync(IEnumerable<DocumentModel> models, Func<IEnumerable<DocumentModelDTO>, Task> success)
        {
            if (!(models.Any()))
            {
                await success(new List<DocumentModelDTO>());
                return;
            }
            var returnedFields = 0;
            var returnedKeys = 0;

            var fieldIds = models.SelectMany(m => m.Fields.Values).Distinct();
            var keyIds = models.SelectMany(m => m.Fields.Keys).Distinct();

            var neededFields = fieldIds.Count();
            var neededKeys = fieldIds.Count();

            var fieldIdsToFields = new Dictionary<string, FieldModelDTO>();
            var keyIdsToKeys = new Dictionary<string, KeyModel>();


            async Task fieldPoll (FieldModelDTO field) 
            {
                returnedFields++;
                fieldIdsToFields[field.Id] = field;
            };

            async Task keyPoll (KeyModel key) 
            {
                returnedKeys++;
                keyIdsToKeys[key.Id] = key;
            };

            foreach (var fieldId in fieldIds)
            {
                await _fields.GetField(fieldId, fieldPoll, (args) => { Debug.WriteLine("Shit"); });
            }

            foreach (var keyId in keyIds)
            {
                await _keys.GetKey(keyId, keyPoll, (args) => { Debug.WriteLine("Shit with keys"); });
            }


            if (returnedFields == neededFields && returnedKeys == neededKeys)
            {
                await CreateModels(models, fieldIdsToFields, keyIdsToKeys, success);
            }
            else
            {
                Debug.WriteLine("fuck");
            }

            Debug.WriteLine("WAITING");
        }

    }
}
