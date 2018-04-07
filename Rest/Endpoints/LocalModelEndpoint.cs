using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{
    public class LocalModelEndpoint<T> : IModelEndpoint<T> where T : EntityBase
    {


        protected Dictionary<string, bool> DirtyDict = new Dictionary<string, bool>();
        /// <summary>
        /// private dictionary here to save your objects in memory.  Should be synced with the local dash files
        /// </summary>
        protected Dictionary<string, string> ModelDictionary;

        /// <summary>
        /// private timer that simple calls a callback every time interval and forces this class to save the current objects
        /// </summary>
        public Timer SaveTimer { get; }

        private readonly string _fileName = "dash." + typeof(T).Name;

        public LocalModelEndpoint()
        {
            SaveTimer = new Timer(SaveTimerCallback, null,
                new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond),
                new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond));
            try
            {
                if (File.Exists(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName))
                {
                    var dictionaryText = File.ReadAllText(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName);
                    ModelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryText);
                    ModelDictionary = ModelDictionary ?? new Dictionary<string, string>();
                    File.Copy(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName, ApplicationData.Current.LocalFolder.Path + "\\" + DateTime.UtcNow.Ticks + "_backup_" + _fileName, true);
                }
                else
                {
                    ModelDictionary = ModelDictionary ?? new Dictionary<string, string>();
                }

                Debug.WriteLine("\n\n\n\nDatabase at:   " + ApplicationData.Current.LocalFolder.Path + "\n\n\n\n");
            }
            catch (Exception e)
            {
                ModelDictionary = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Event handler called every tme interval that saves the current version of your objects
        /// </summary>
        /// <param name="state"></param>
        private async void SaveTimerCallback(object state)
        {
            try
            {
                if (ModelDictionary != null)
                {
                    var d = new Dictionary<string, string>(ModelDictionary);
                    var dirty = new Dictionary<string, bool>(DirtyDict);
                    foreach (var b in dirty)
                    {
                        if (b.Value)
                        {
                            var controller = ContentController<T>.GetModel((b.Key));
                            var json = controller.Serialize();
                            d[b.Key] = json;
                        }
                    }

                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("temp_" + _fileName, CreationCollisionOption.ReplaceExisting);
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        using (var outgoingStream = stream.GetOutputStreamAt(0))
                        {
                            using (var dw = new DataWriter(outgoingStream))
                            {
                                dw.WriteString(JsonConvert.SerializeObject(d));
                                await dw.StoreAsync();
                                await dw.FlushAsync();
                            }
                        }
                    }
                    await file.RenameAsync(_fileName, NameCollisionOption.ReplaceExisting);
                    ModelDictionary = d;
                    DirtyDict = new Dictionary<string, bool>();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public virtual void AddDocument(T newDocument, Action<T> success, Action<Exception> error)
        {
            DirtyDict[newDocument.Id] = true;
        }

        public virtual void UpdateDocument(T documentToUpdate, Action<T> success, Action<Exception> error)
        {
            DirtyDict[documentToUpdate.Id] = true;
        }

        protected string GetModel(string id)
        {
           if (ModelDictionary.ContainsKey(id))
                return ModelDictionary[id];

            if (DirtyDict.ContainsKey(id))
            {
                var model = ContentController<T>.GetModel((id));
                var json = model.Serialize();
                ModelDictionary[id] = json;
                DirtyDict.Remove((id));
            }
            return ModelDictionary[id];
        }
        public virtual async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var doc = GetModel(id);
                var args = new RestRequestReturnArgs()
                {
                    ReturnedObjects = new List<EntityBase>()
                    {
                        doc.CreateObject<T>()
                    }
                };
                await success?.Invoke(args);
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        public virtual async Task GetDocuments(IEnumerable<string> ids, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var list = new List<T>();
                foreach (var id in ids)
                {
                    var text = GetModel(id);
                    var doc = text.CreateObject<T>();
                    list.Add(doc);
                }
                await success?.Invoke(new RestRequestReturnArgs(list));
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        public virtual void DeleteDocument(T document, Action success, Action<Exception> error)
        {
            try
            {
                ModelDictionary.Remove(document.Id);
                DirtyDict.Remove(document.Id);
                success?.Invoke();
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        public virtual void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            ModelDictionary = new Dictionary<string, string>();
            DirtyDict = new Dictionary<string, bool>();
            success?.Invoke();
        }

        public virtual async Task GetDocumentsByQuery(IQuery<T> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                await success?.Invoke(new RestRequestReturnArgs(ModelDictionary.Values.Select(i => i.CreateObject<T>()).Where(query.Func)));
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        private Func<RestRequestReturnArgs, Task> GetCastingFunc<V>(Func<IEnumerable<V>, Task> previousFunc) where V : EntityBase
        {
            async Task func(RestRequestReturnArgs args)
            {
                await previousFunc(args.ReturnedObjects.OfType<V>());
            }

            return func;
        }

        public virtual async Task GetDocuments<V>(IEnumerable<string> ids, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            await GetDocuments(ids, GetCastingFunc(success), error);
        }

        public virtual async Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            await GetDocumentsByQuery(query, GetCastingFunc(success), error);
        }

        /// <summary>
        /// Close the connection to the Endpoint
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
        }
    }
}
