using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using DashShared;
using DashShared.Models;
using Newtonsoft.Json;

namespace Dash
{
    public class LocalFieldEndpoint : IFieldEndpoint
    {
        /// <summary>
        /// private dictionary here to save your objects in memory.  Should be synced with the local dash files
        /// </summary>
        private Dictionary<string, string> _modelDictionary;

        /// <summary>
        /// private timer that simple calls a callback every time interval and forces this class to save the current objects
        /// </summary>
        private Timer _saveTimer;
        public LocalFieldEndpoint()
        {
            _saveTimer = new Timer(SaveTimerCallback, null, new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond), new TimeSpan(DashConstants.MillisecondBetweenLocalSave * TimeSpan.TicksPerMillisecond));
            try
            {
                var dictionaryText = File.ReadAllText(LocalKeyEndpoint.LocalStorageFolder.Path + "\\"+ DashConstants.LocalServerFieldFilepath);
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
            var file = await LocalKeyEndpoint.LocalStorageFolder.CreateFileAsync(DashConstants.LocalServerFieldFilepath,CreationCollisionOption.OpenIfExists);
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
        public void AddField(FieldModel newField, Action<FieldModel> success, Action<Exception> error)
        {
            try
            {
                _modelDictionary[newField.Id] = newField.Serialize();
                success(_modelDictionary[newField.Id].CreateObject<FieldModel>());
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public void UpdateField(FieldModel fieldToUpdate, Action<FieldModel> success, Action<Exception> error)
        {
            AddField(fieldToUpdate, success,error);
        }

        public async Task GetField(string id, Func<FieldModel, Task> success, Action<Exception> error)
        {
            try
            {
                var model = _modelDictionary[id].CreateObject<FieldModel>();
                await success(model);
            }
            catch (Exception e)
            {
                error(e);
            }
        }

        public async Task DeleteField(FieldModel fieldToDelete, Action success, Action<Exception> error)
        {
            try
            {
                _modelDictionary.Remove(fieldToDelete.Id);
                success();
            }
            catch (Exception e)
            {
                error(e);
            }
        }
    }
}
