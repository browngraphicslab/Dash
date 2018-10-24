using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Newtonsoft.Json;

namespace Dash
{

    public class InkController : FieldModelController<InkModel>
    {
        private InkStrokeContainer _strokeContainer = new InkStrokeContainer();

        public InkController() : base(new InkModel())
        {
            UpdateStrokesFromList(new List<InkStroke>(), false);
        }

        public InkController(string inkData) : base(new InkModel(inkData))
        {
            UpdateStrokesFromData(inkData);
        }

        public InkController(InkModel inkFieldModel) : base(inkFieldModel)
        {
            UpdateStrokesFromData(inkFieldModel.Data);
        }

        /// <summary>
        ///     The <see cref="InkFieldModel" /> associated with this <see cref="InkController" />,
        ///     You should only set values on the controller, never directly on the model!
        /// </summary>
        public InkModel InkFieldModel => Model as InkModel;


        public string InkData => InkFieldModel.Data;

        public override object GetValue(Context context)
        {
            return InkData;
        }
        public override bool TrySetValue(object value)
        {
            switch (value)
            {
                case IEnumerable<InkStroke> cont:
                    UpdateStrokesFromList(cont);
                    return true;
            }

            return false;
        }

        public override FieldControllerBase Copy()
        {
            return new InkController(InkData);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new InkController();
        }

        /// <summary>
        /// Method to allow InkCanvasControls to change data of InkFieldModelController when ink input is registered.
        /// </summary>
        public async void UpdateStrokesFromList(IEnumerable<InkStroke> newStrokes, bool withUndo = true)
        {
            IEnumerable<InkStroke> oldStrokes = _strokeContainer.GetStrokes();
            _strokeContainer.Clear();
            var inkStrokes = newStrokes.ToList();
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
            string data = JsonConvert.SerializeObject(stream.ToArray());
            stream.Dispose();
            var newEvent = new UndoCommand(() => UpdateStrokesFromList(inkStrokes, false), () => UpdateStrokesFromList(oldStrokes, false));

            InkFieldModel.Data = data;
            UpdateOnServer(withUndo ? newEvent : null);
            OnFieldModelUpdated(null);
        }

        private async void UpdateStrokesFromData(string s)
        {
            var bytes = JsonConvert.DeserializeObject<byte[]>(s);
            _strokeContainer.Clear();
            if (bytes != null)
            {
                var stream = new MemoryStream(bytes);
                using (var inputStream = stream.AsInputStream())
                {
                    await _strokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
        }

        public override StringSearchModel SearchForString(string searchString)
        {
            return StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "InkController";
        }

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
