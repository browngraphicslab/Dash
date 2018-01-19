using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Accord.Imaging.ColorReduction;
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

        //Output keys
        public static readonly KeyController PaletteKey = new KeyController("7431D567-7582-477B-A372-5964C2D26AE6", "Sum");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [ImageKey] = new IOInfo(TypeInfo.Image, true),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [PaletteKey] = TypeInfo.Number,
        };

        public override async void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {

            var inputImage = inputs[ImageKey] as ImageController;

            var stream = inputImage.ImageSource.IsFile ? File.OpenRead(inputImage.ImageSource.LocalPath).AsRandomAccessStream() : inputImage.ImageSource.GetHttpStreamAsync().Result;
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var writeBitmap = new WriteableBitmap(Convert.ToInt32(decoder.PixelWidth), Convert.ToInt32(decoder.PixelHeight));
            stream.Seek(0);
            stream = inputImage.ImageSource.IsFile ? File.OpenRead(inputImage.ImageSource.LocalPath).AsRandomAccessStream() : inputImage.ImageSource.GetHttpStreamAsync().Result;
            await writeBitmap.SetSourceAsync(stream);
            var bitmap = (Bitmap)writeBitmap;


            var quantizer = new ColorImageQuantizer(new MedianCutQuantizer());
            var colors = quantizer.CalculatePalette(bitmap, 5);
            

            outputs[PaletteKey] = new NumberController(1);
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
