using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using Accord.Imaging.ColorReduction;
using Accord.IO;
using DashShared;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Dash
{
    public class ImageToColorPalette : OperatorController
    {
        public ImageToColorPalette(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ImageToColorPalette() : base(new OperatorModel(OperatorType.ImageToColorPalette))
        {
        }

        //Input keys
        public static readonly KeyController ImageKey = new KeyController("942F7A38-3E5D-4CD7-9A88-C61B962511B8", "Input Image");
        public static readonly KeyController FakeAsyncInput = new KeyController("EE21E035-DBE1-4614-97C4-7AA4C76DB656", "Fake Async Input Palette");

        //Output keys
        public static readonly KeyController PaletteKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Color Palette");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(ImageKey, new IOInfo(TypeInfo.Image, true)),
            new KeyValuePair<KeyController, IOInfo>(FakeAsyncInput, new IOInfo(TypeInfo.List, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [PaletteKey] = TypeInfo.List
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {

            int numColors = 5;

            if (inputs.ContainsKey(FakeAsyncInput))
            {
                var coll = inputs[FakeAsyncInput] as ListController<DocumentController>;
                if (coll.Data.Count > 0 && coll.Data.Count <= numColors)
                {
                    outputs[PaletteKey] = coll;
                    return;
                }
            }

            UITask.RunTask(async () =>
            {
                var outputCollection = new List<FieldControllerBase>();
                var inputImage = inputs[ImageKey] as ImageController;
                var stream = inputImage.ImageSource.IsFile
                    ? File.OpenRead(inputImage.ImageSource.LocalPath).AsRandomAccessStream()
                    : await inputImage.ImageSource.GetHttpStreamAsync();
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var writeBitmap = new WriteableBitmap(Convert.ToInt32(decoder.PixelWidth),
                    Convert.ToInt32(decoder.PixelHeight));
                stream.Seek(0);
                //stream = inputImage.ImageSource.IsFile ? File.OpenRead(inputImage.ImageSource.LocalPath).AsRandomAccessStream() : inputImage.ImageSource.GetHttpStreamAsync().Result;
                await writeBitmap.SetSourceAsync(stream);
                var bitmap = (Bitmap)writeBitmap;

                var quantizer = new ColorImageQuantizer(new MedianCutQuantizer());
                var colors = quantizer.CalculatePalette(bitmap, numColors);

                foreach (var color in colors)
                {
                    var bmp = new WriteableBitmap(128, 128);
                    var bmpStream = bmp.PixelBuffer.AsStream();
                    for (var x = 0; x < bmp.PixelWidth; x++)
                    {
                        for (var y = 0; y < bmp.PixelHeight; y++)
                        {
                            bmpStream.WriteByte(color.B);
                            bmpStream.WriteByte(color.G);
                            bmpStream.WriteByte(color.R);
                            bmpStream.WriteByte(color.A);
                        }
                    }

                    outputCollection.Add(await new ImageToDashUtil().ParseBitmapAsync(bmp));
                }

                (inputs[FakeAsyncInput] as ListController<DocumentController>).Data = outputCollection;
            });
        }



        public override FieldModelController<OperatorModel> Copy()
        {
            return new ImageToColorPalette();
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }
    }
}
