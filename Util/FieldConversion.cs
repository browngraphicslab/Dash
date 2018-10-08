using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml.Data;
using Dash.Controllers;
using Dash.Converters;

namespace Dash
{
    public static class FieldConversion
    {
        public static IValueConverter GetFieldtoStringConverter(FieldControllerBase controller)
        {
            if (controller is DocumentController)
            {
                return new DocumentControllerToStringConverter();
            }
            if (controller is ListController<DocumentController>)
            {
                return new DocumentCollectionToStringConverter();
            }
            if (controller is NumberController)
            {
                return new DoubleToStringConverter();
            }
            if (controller is PointController)
            {
                return new PointToStringConverter();
            }
            if (controller is ImageController || controller is VideoController || controller is AudioController)
            {
                return new UriToStringConverter();
            }
            if (controller is DateTimeController)
            {
                return new DateTimeToStringConverter();
            }
            return new ObjectToStringConverter(null);
        }

        public static string ConvertFieldToString(FieldControllerBase controller, Context context = null)
        {
            return GetFieldtoStringConverter(controller).Convert(controller.GetValue(context), typeof(string), null, "en") as string;
        }

        public static void SetFieldtoString(FieldControllerBase controller, string data, Context context)
        {

            controller = controller.DereferenceToRoot(context);

            bool converted = false;
            if (controller is DocumentController dc)
            {
                dc.TrySetValue(new DocumentControllerToStringConverter(context).ConvertXamlToData(data));
                converted = true;
            }
            else if (controller is ListController<DocumentController> ldc)
            {
                ldc.Data = new List<FieldControllerBase>(new DocumentCollectionToStringConverter(context).ConvertXamlToData(data));
                converted = true;

            }
            else if (controller is NumberController nc)
            {
                nc.Data = (new DoubleToStringConverter().ConvertXamlToData(data));
                converted = true;

            }
            else if (controller is PointController pc)
            {
                pc.Data = (new PointToStringConverter().ConvertXamlToData(data));
                converted = true;

            }
            else if (controller is ImageController ic)
            {
                ic.Data = (new UriToStringConverter().ConvertXamlToData(data));
                converted = true;

            }
            else if (controller is TextController tc)
            {
                tc.Data = data;
                converted = true;

            }
            else if (controller is RichTextController rtc)
            {
                // TODO write a converter for this? I think this causes rich text to lose formatting
                rtc.Data = new RichTextModel.RTD(data);
                converted = true;

            }
            if (!converted)
            {
                throw new NotImplementedException();
            }
        }

        // TODO remove this
        public static FieldControllerBase StringToFieldModelController(string data)
        {
            // check for number field model controller
            var num = IsNumeric(data);
            if (num.HasValue)
                return new NumberController(num.Value);

            string[] imageExtensions = { "jpg", "bmp", "gif", "png" }; //  etc

            if (imageExtensions.Any(data.EndsWith))
                return new ImageController(new Uri(data));
            if (data.EndsWith("pdf"))
            {
                return new PdfController(new Uri(data));
            }
            return new TextController(data);


            double? IsNumeric(string expression)
            {
                var isNum = double.TryParse(expression, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out double ret);
                if (isNum)
                    return ret;
                return null;
            }


        }
    }
}
