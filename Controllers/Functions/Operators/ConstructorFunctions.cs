using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dash.Controllers;

namespace Dash
{
    public static class ConstructorFunctions
    {
        [OperatorReturnName("Point")]
        [OperatorFunctionName("point")]
        public static PointController ZeroPoint()
        {
            return new PointController();
        }

        [OperatorReturnName("Point")]
        public static PointController Point(NumberController x, NumberController y)
        {
            return new PointController(x.Data, y.Data);
        }

        [OperatorReturnName("Image")]
        public static ImageController Image(TextController path)
        {
            try
            {
                return new ImageController(new Uri(path.Data));
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new ImageCreationFailureErrorModel(path.Data));
            }   
        }

        [OperatorReturnName("Video")]
        public static VideoController Video(TextController path)
        {
            try
            {
                return new VideoController(new Uri(path.Data));
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new ImageCreationFailureErrorModel(path.Data));
            }   
        }

        [OperatorReturnName("Audio")]
        public static AudioController Audio(TextController path)
        {
            try
            {
                return new AudioController(new Uri(path.Data));
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new ImageCreationFailureErrorModel(path.Data));
            }   
        }

        [OperatorReturnName("Pdf")]
        public static PdfController Pdf(TextController path)
        {
            try
            {
                return new PdfController(new Uri(path.Data));
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new ImageCreationFailureErrorModel(path.Data));
            }   
        }

        [OperatorReturnName("Color")]
        public static ColorController Color(TextController s)
        {
            return new ColorController(ColorConverter.HexToColor(s.Data));
        }

        [OperatorReturnName("Date")]
        public static DateTimeController Date(TextController s)
        {
            return new DateTimeController(DateTime.Parse(s.Data));
        }
    }
}
