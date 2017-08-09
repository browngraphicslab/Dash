using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace Dash.Converters
{
    public class StringToInkConverter : SafeDataToXamlConverter<string, InkStrokeContainer>
    {
        public static StringToInkConverter Instance = new StringToInkConverter();

        public override InkStrokeContainer ConvertDataToXaml(string data, object parameter = null)
        {
            if(data == null || data == "") return new InkStrokeContainer();
            var deserializedBytes = JsonConvert.DeserializeObject<byte[]>(data);
            MemoryStream stream = new MemoryStream(deserializedBytes);
            var inkCanvas = new InkCanvas();
            var task = Task.Run(async () =>
            {
                using (var inputStream = stream.AsInputStream())
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
                return inkCanvas.InkPresenter.StrokeContainer;
            });
            return task.Result;

        }

        public override string ConvertXamlToData(InkStrokeContainer inkStrokeContainer, object parameter = null)
        {
            MemoryStream stream = new MemoryStream();
            var task = Task.Run(async () =>
            {
                using (IOutputStream outputStream = stream.AsOutputStream())
                {
                    await inkStrokeContainer.SaveAsync(outputStream);
                    await outputStream.FlushAsync();
                }
                return JsonConvert.SerializeObject(stream.ToArray());
            });
            return task.Result;
        }
    }
}
