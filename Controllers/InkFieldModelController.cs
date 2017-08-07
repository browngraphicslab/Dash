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

        public InkFieldModelController() : base(new InkFieldModel())
        {
        }

        public InkFieldModelController(string data) : base(new InkFieldModel(data))
        {
            InkData = data;
            UpdateStrokesData(GetStrokesFromJSON(data));
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
                OnFieldModelUpdated(null);
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            var inkFieldModelController = fieldModel as InkFieldModelController;
            if (inkFieldModelController != null) InkData = inkFieldModelController.InkData;
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            var inkCanvas = new InkCanvas()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var ctrls = new InkCanvasControls(inkCanvas, this);

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

        public IEnumerable<InkStroke> GetStrokesFromJSON(string json)
        {
            var inkStrokes = JsonConvert.DeserializeObject<IEnumerable<InkStroke>>(json);
            return inkStrokes;
        }

        /// <summary>
        /// Method to allow InkCanvasControls to change data of InkFieldModelController when ink input is registered.
        /// </summary>
        public async void UpdateStrokesData(IEnumerable<InkStroke> newStrokes)
        {
            _strokeContainer.Clear();
            foreach (var newStroke in newStrokes)
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
            stream.Dispose();
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
