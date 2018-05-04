﻿using System;
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


        protected Dictionary<string, bool> _dirtyDict = new Dictionary<string, bool>();
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
                if (File.Exists(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName))
                {
                    var dictionaryText = File.ReadAllText(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName);
                    _modelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryText);
                    _modelDictionary = _modelDictionary ?? new Dictionary<string, string>();
                    File.Copy(ApplicationData.Current.LocalFolder.Path + "\\" + _fileName, ApplicationData.Current.LocalFolder.Path + "\\" + DateTime.UtcNow.Ticks + "_backup_" + _fileName, true);
                }
                else
                {
                    _modelDictionary = _modelDictionary ?? new Dictionary<string, string>();
                }

                Debug.WriteLine("\n\n\n\nDatabase at:   " + ApplicationData.Current.LocalFolder.Path + "\n\n\n\n");
            }
            catch (Exception e)
            {
                _modelDictionary = new Dictionary<string, string>();
            }
            //App.Instance.Suspending += AppSuspending;
        }

        public Dictionary<string, string> GetBackups()
        {
            var dict = new Dictionary<string, string>();

            var directoryInfo = new DirectoryInfo(ApplicationData.Current.LocalFolder.Path);
            var fileInfos = directoryInfo.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                if (!fileInfo.Name.Contains("_backup_")) continue;
                var splitInfo = fileInfo.Name.Split(new[] { "_backup_" }, StringSplitOptions.None);
                var ticks = long.Parse(splitInfo[0]);
                var prettyTime = new DateTime(ticks).ToString("MM/dd/yyyy h:mm tt");
                dict[prettyTime] = fileInfo.FullName;
            }

            return dict;
        }

        /// <summary>
        /// Event handler called every tme interval that saves the current version of your objects
        /// </summary>
        /// <param name="state"></param>
        private async void SaveTimerCallback(object state)
        {
            try
            {
                if (_modelDictionary != null)
                {
                    var d = new Dictionary<string, string>(_modelDictionary);
                    var dirty = new Dictionary<string, bool>(_dirtyDict);
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
                    _modelDictionary = d;
                    _dirtyDict = new Dictionary<string, bool>();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
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
            _dirtyDict[newDocument.Id] = true;
            //var json = newDocument.Serialize();
            //if (typeof(T) != typeof(KeyModel) && !_modelDictionary.ContainsKey(newDocument.Id))
            //{

            //}
            //_modelDictionary[newDocument.Id] = json;
            //success?.Invoke(json.CreateObject<T>());
        }

        public virtual void UpdateDocument(T documentToUpdate, Action<T> success, Action<Exception> error)
        {
            _dirtyDict[documentToUpdate.Id] = true;
            //if (_modelDictionary.ContainsKey(documentToUpdate.Id))
            //{
            //    var json = documentToUpdate.Serialize();
            //    _modelDictionary[documentToUpdate.Id] = json;
            //    success?.Invoke(json.CreateObject<T>());
            //}
            //else
            //{
            //    //error?.Invoke(new Exception("The document didn't exist!"));
            //}
        }

        protected string GetModel(string id)
        {
           if (_modelDictionary.ContainsKey(id))
                return _modelDictionary[id];

            if (_dirtyDict.ContainsKey(id))
            {
                var model = ContentController<T>.GetModel((id));
                var json = model.Serialize();
                _modelDictionary[id] = json;
                _dirtyDict.Remove((id));
            }
            return _modelDictionary[id];
        }
        public virtual async Task GetDocument(string id, Func<RestRequestReturnArgs, Task> success, Action<Exception> error)
        {
            try
            {
                var doc = GetModel(id);
                var args = new RestRequestReturnArgs
                {
                    ReturnedObjects = new List<EntityBase>
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
                _modelDictionary.Remove(document.Id);
                _dirtyDict.Remove(document.Id);
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
            _dirtyDict = new Dictionary<string, bool>();
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

        public virtual async Task GetDocumentsByQuery<V>(IQuery<T> query, Func<IEnumerable<V>, Task> success, Action<Exception> error) where V : EntityBase
        {
            await GetDocumentsByQuery(query, GetCastingFunc(success), error);
        }

    }
}
