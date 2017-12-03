using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Converters;
using Dash.Models;
using Dash.Views;
using DashShared;
using DashShared.Models;
using Newtonsoft.Json;

namespace Dash
{
    
    public class InkController : FieldModelController<InkModel>
    {
        private InkStrokeContainer _strokeContainer = new InkStrokeContainer();
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();
        private Image _icon = new Image();


        public delegate void InkUpdatedHandler(InkCanvas sender, FieldUpdatedEventArgs args);
        public event InkUpdatedHandler InkUpdated;

        public InkController() : base(new InkModel())
        {
            UpdateStrokesFromList(null, null);
        }

        public InkController(string data) : base(new InkModel(data))
        {
            InkData = data;
            _undoStack.Push(data);
            SetState(data, null);
        }

        public InkController(InkModel inkFieldModel) : base(inkFieldModel)
        {
            InkData = inkFieldModel.Data;
            _undoStack.Push(inkFieldModel.Data);
            SetState(inkFieldModel.Data, null);
        }
        public override void Init()
        {

        }

        /// <summary>
        ///     The <see cref="InkFieldModel" /> associated with this <see cref="InkController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public InkModel InkFieldModel => Model as InkModel;

        
        public string InkData
        {
            get { return InkFieldModel.Data; }
            set
            {
                if (InkFieldModel.Data != value)
                {
                    InkFieldModel.Data = value;
                    // Update the server
                    UpdateOnServer();
                }
            }
        }

        //TODO needs work
        public  override FrameworkElement GetTableCellView(Context context)
        {
            return new Grid();
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }

        public override FieldModelController<InkModel> Copy()
        {
            return new InkController(InkData);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new InkController();
        }

        /// <summary>
        /// Method to allow InkCanvasControls to change data of InkController when ink input is registered.
        /// </summary>
        public async void UpdateStrokesFromList(IEnumerable<InkStroke> newStrokes, InkCanvas sender)
        {
            
            _strokeContainer.Clear();
            var inkStrokes = newStrokes == null ?  new InkStroke[0] : newStrokes.ToArray();
            foreach (var newStroke in inkStrokes)
            {
                _strokeContainer.AddStroke(newStroke.Clone());
            }
            MemoryStream stream = new MemoryStream();
            using (IOutputStream outputStream = stream.AsOutputStream())
            {
                await _strokeContainer.SaveAsync(outputStream);
                await outputStream.FlushAsync();
            }
            InkData = JsonConvert.SerializeObject(stream.ToArray());
            //_redoStack.Clear();
            //_undoStack.Push(InkData);
            stream.Dispose();
            var args = new FieldUpdatedEventArgs(TypeInfo.Ink, DocumentController.FieldUpdatedAction.Update);
            OnFieldModelUpdated(args);
            if (sender != null) InkUpdated?.Invoke(sender, args);
        }

        public async void SetState(string data, InkCanvas sender)
        {
            try
            {
                var bytes = JsonConvert.DeserializeObject<byte[]>(data);
                _strokeContainer.Clear();
                MemoryStream stream = new MemoryStream(bytes);
                using (var inputStream = stream.AsInputStream())
                {
                    await _strokeContainer.LoadAsync(inputStream);
                }
                if (_strokeContainer.GetStrokes().Count > 0) IsEmpty = false;
                stream.Dispose();
                InkData = data;
                var args = new FieldUpdatedEventArgs(TypeInfo.Ink, DocumentController.FieldUpdatedAction.Replace);
                OnFieldModelUpdated(args);
                if (sender != null) InkUpdated?.Invoke(sender, args);
            }
            catch (JsonSerializationException e)
            {
                Debug.Fail("Json failed to parse");
            }
        }

        public void Redo(InkCanvas sender)
        {
            if (_redoStack.Count > 0)
            {
                var state = _redoStack.Pop();
                SetState(state, sender);
                _undoStack.Push(state);
            }
        }

        public void Undo(InkCanvas sender)
        {
            if (_undoStack.Count > 1)
            {
                var state = _undoStack.Pop();
                SetState(_undoStack.Peek(), sender);
                _redoStack.Push(state);
            }
        }

        public bool IsEmpty { get; set; }


        public IReadOnlyList<InkStroke> GetStrokes()
        {
            return _strokeContainer.GetStrokes();
        }

        public override TypeInfo TypeInfo => TypeInfo.Ink;

        public override string ToString()
        {
            return "Ink";
        }
    }
}
