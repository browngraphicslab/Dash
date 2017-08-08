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
using Newtonsoft.Json;

namespace Dash
{
    
    public class InkFieldModelController : FieldModelController
    {
        private InkStrokeContainer _strokeContainer = new InkStrokeContainer();
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();
        private Image _icon = new Image();

        public InkFieldModelController() : base(new InkFieldModel())
        {
            UpdateStrokesFromList(null);
        }

        public InkFieldModelController(string data) : base(new InkFieldModel(data))
        {
            InkData = data;
            _undoStack.Push(data);
            SetState(data);
        }

        /// <summary>
        ///     The <see cref="InkFieldModel" /> associated with this <see cref="InkFieldModelController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public InkFieldModel InkFieldModel => FieldModel as InkFieldModel;

        
        public string InkData
        {
            get { return InkFieldModel.Data; }
            set
            {
                if (SetProperty(ref InkFieldModel.Data, value))
                {
                    // update local
                    // update server    
                }
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            var inkFieldModelController = fieldModel as InkFieldModelController;
            if (inkFieldModelController != null) InkData = inkFieldModelController.InkData;
        }

        //TODO needs work
        public  override FrameworkElement GetTableCellView(Context context)
        {
            var inkCanvas = new InkCanvas() { Width = 50, Height = 50 };
            inkCanvas.InkPresenter.StrokeContainer.AddStrokes(GetStrokes().Select(k => k.Clone()));
            return inkCanvas;
        }

        public override FieldModelController Copy()
        {
            throw new NotImplementedException();
        }

        public override FieldModelController GetDefaultController()
        {
            return new InkFieldModelController();
        }

        /// <summary>
        /// Method to allow InkCanvasControls to change data of InkFieldModelController when ink input is registered.
        /// </summary>
        public async void UpdateStrokesFromList(IEnumerable<InkStroke> newStrokes)
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
            _redoStack.Clear();
            _undoStack.Push(InkData);
            stream.Dispose();
            OnFieldModelUpdated(new FieldUpdatedEventArgs(TypeInfo.Ink, DocumentController.FieldUpdatedAction.Update));
        }

        public async void SetState(string data)
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
                OnFieldModelUpdated(new FieldUpdatedEventArgs(TypeInfo.Ink, DocumentController.FieldUpdatedAction.Replace));
            }
            catch (JsonSerializationException e)
            {
                Debug.Fail("Json failed to parse");
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var state = _redoStack.Pop();
                SetState(state);
                _undoStack.Push(state);
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 1)
            {
                var state = _undoStack.Pop();
                SetState(_undoStack.Peek());
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
