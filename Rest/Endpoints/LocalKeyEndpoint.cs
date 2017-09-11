using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DashShared;
using Newtonsoft.Json;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Dash
{
    public class LocalKeyEndpoint : IKeyEndpoint
    {
        /// <summary>
        /// private dictionary here to save your objects in memory.  Should be synced with the local dash files
        /// </summary>
        private Dictionary<string, string> _modelDictionary;

        /// <summary>
        /// private timer that simple calls a callback every time interval and forces this class to save the current objects
        /// </summary>
        private Timer _saveTimer;
        public LocalKeyEndpoint()
        {
            _saveTimer = new Timer(SaveTimerCallback, null, new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond), new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond));
            try
            {
                var dictionaryText = File.ReadAllText(DashConstants.LocalStorageFolder.Path + "\\"+ DashConstants.LocalServerKeyFilepath);
                _modelDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryText);
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
            var file = await DashConstants.LocalStorageFolder.CreateFileAsync(DashConstants.LocalServerKeyFilepath,CreationCollisionOption.OpenIfExists);
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

        public void AddKey(KeyModel newKey, Action<KeyModel> success, Action<Exception> error)
        {
            try
            {
                _modelDictionary[newKey.Id] = JsonConvert.SerializeObject(newKey);
                success(JsonConvert.DeserializeObject<KeyModel>(_modelDictionary[newKey.Id]));
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void UpdateKey(KeyModel keyToUpdate, Action<KeyModel> success, Action<Exception> error)
        {
            AddKey(keyToUpdate, success, error);
        }

        public void GetKey(string id, Action<KeyModel> success, Action<Exception> error)
        {
            try
            {
                success(JsonConvert.DeserializeObject<KeyModel>(_modelDictionary[id]));
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void DeleteKey(KeyModel keyToDelete, Action success, Action<Exception> error)
        {
            try
            {
                _modelDictionary.Remove(keyToDelete.Id);
                success();
            }
            catch (Exception e)
            {
                error(e);
            }
        }
    }
}
