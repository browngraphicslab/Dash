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
using Newtonsoft.Json;

namespace Dash
{
    public class InkFieldModelController : FieldModelController
    {
        

        public InkFieldModelController() : base(new InkFieldModel()) { UpdateStrokes(); }

        public InkFieldModelController(string data) : base(new InkFieldModel(data)) { UpdateStrokes(); }

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
                FireFieldModelUpdated();
            }
        }

        protected override void UpdateValue(FieldModelController fieldModel)
        {
            var inkFieldModelController = fieldModel as InkFieldModelController;
            if (inkFieldModelController != null) InkData = inkFieldModelController.InkData;
        }
        public override FrameworkElement GetTableCellView()
        {
            var inkCanvasControl = new InkCanvasControl(this)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            return inkCanvasControl;
        }

        /// <summary>
        /// Converts the locally stored InkStrokes into a string and sets the field model's data to that string.
        /// </summary>
        public async void UpdateData()
        {
            if (_strokeSet.Count != 0)
            {
                MemoryStream stream = new MemoryStream();
                var strokeContainer = new InkStrokeContainer();
                foreach (var stroke in _strokeSet)
                {
                    strokeContainer.AddStroke(stroke.Clone());
                }
                using (IOutputStream outputStream = stream.AsOutputStream())
                {
                    await strokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                InkData = JsonConvert.SerializeObject(stream.ToArray());
            }
            else
            {
                InkData = null;
            }
            
        }

        /// <summary>
        /// Converts InkFieldModel's Data to inkStrokes, which are stored in the stroke set
        /// </summary>
        public async void UpdateStrokes()
        {
            if (InkFieldModel.Data != null)
            {
                var deserializedBytes = JsonConvert.DeserializeObject<byte[]>(InkFieldModel.Data);
                if (deserializedBytes != null)
                {
                    var strokeContainer = new InkStrokeContainer();
                    MemoryStream stream = new MemoryStream(deserializedBytes);
                    using (var inputStream = stream.AsInputStream())
                    {
                        await strokeContainer.LoadAsync(inputStream);
                    }
                    stream.Dispose();
                    _strokeSet = new HashSet<InkStroke>(strokeContainer.GetStrokes());
                }
            }
        }

        /// <summary>
        /// A HashSet of strokes that can be edited locally without changing the field model's data
        /// </summary>
        private HashSet<InkStroke> _strokeSet = new HashSet<InkStroke>();

        public void SetStrokes(IEnumerable<InkStroke> strokes)
        {
            _strokeSet = new HashSet<InkStroke>(strokes);
            UpdateData();
        }

        public IEnumerable<InkStroke> GetStrokes()
        {
            return _strokeSet;
        }

        public override TypeInfo TypeInfo => TypeInfo.Image;

        public override string ToString()
        {
            return "Ink";
        }
    }
}
