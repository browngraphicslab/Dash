using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// private dictionary here to save your objects in memory.  Should be synced with the local dash files
        /// </summary>
        protected Dictionary<string, string> _modelDictionary;

        /// <summary>
        /// private timer that simple calls a callback every time interval and forces this class to save the current objects
        /// </summary>
        private Timer _saveTimer;

        private string _fileName = "dash." + typeof(T).Name;

        public LocalModelEndpoint()
        {
            _saveTimer = new Timer(SaveTimerCallback, null,
                new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond),
                new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond));
            try
            {
                var dictionaryText = File.ReadAllText(DashConstants.LocalStorageFolder.Path + "\\" + _fileName);
                _modelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryText);
                _modelDictionary = _modelDictionary ?? new Dictionary<string, string>();
            }
            catch (Exception e)
            {
                _modelDictionary = new Dictionary<string, string>();
            }
            //App.Instance.Suspending += AppSuspending;
        }

        /// <summary>
        /// Event handler called every tme interval that saves the current version of your objects
        /// </summary>
        /// <param name="state"></param>
        private async void SaveTimerCallback(object state)
        {
            try
            {
                var d = new Dictionary<string, string>(_modelDictionary);
                var file = await DashConstants.LocalStorageFolder.CreateFileAsync(_fileName,
                    CreationCollisionOption.ReplaceExisting);
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
            }
            catch (Exception e)
            {
                
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

        public virtual void AddDocument(T newDocument, Action<T> success, Action<Exception> error)
        {
            var json = newDocument.Serialize();
            _modelDictionary[newDocument.Id] = json;
            success?.Invoke(json.CreateObject<T>());
        }

        public virtual void UpdateDocument(T documentToUpdate, Action<T> success, Action<Exception> error)
        {
            var json = documentToUpdate.Serialize();
            _modelDictionary[documentToUpdate.Id] = json;
            success?.Invoke(json.CreateObject<T>());
        }

        public virtual async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var doc = _modelDictionary[id];
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
                    var text = _modelDictionary[id];
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
                _modelDictionary.Remove(document.Id);
                success?.Invoke();
            }
            catch (Exception e)
            {
                error?.Invoke(e);
            }
        }

        public virtual void DeleteAllDocuments(Action success, Action<Exception> error)
        {
            _modelDictionary = new Dictionary<string, string>();
            success?.Invoke();
        }

        public virtual async Task GetDocumentsByQuery(IQuery<T> query, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                await success?.Invoke(new RestRequestReturnArgs(_modelDictionary.Values.Select(i => i.CreateObject<T>()).Where(query.Func)));
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

        public virtual async Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V :EntityBase
        {
            await GetDocumentsByQuery(query, GetCastingFunc(success), error);
        }

    }
}
