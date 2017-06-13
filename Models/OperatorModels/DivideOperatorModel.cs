using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;



namespace Dash
{
    public class DivideOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("AAC1631C-9DC3-48FC-984A-EE0D80C9A397", "Input A");
        public static readonly Key BKey = new Key("A757D709-8D83-44C9-B047-D5DB6420F51F", "Input B");

        //Output keys
        public static readonly Key QuotientKey = new Key("DA705E3D-4773-4C7D-B770-536BA321D0FA", "Quotient");

        public DivideOperatorModel()
        {
        }
        public override Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences)
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentController>();
            Dictionary<Key, FieldModel> result = new Dictionary<Key, FieldModel>(1);

            NumberFieldModel numberA = docController.GetFieldInDocument(inputReferences[AKey]) as NumberFieldModel;
            Debug.Assert(numberA != null, "Input is not a number");

            NumberFieldModel numberB = docController.GetFieldInDocument(inputReferences[BKey]) as NumberFieldModel;
            Debug.Assert(numberB != null, "Input is not a number");

            double a = numberA.Data;
            double b = numberB.Data;
            result[QuotientKey] = new NumberFieldModel(a / b);
            return result;
        }

        public override UIElement MakeView(TemplateModel template)
        {
            Rectangle rect = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Teal),
                Width = 100,
                Height = 100
            };

            return rect;
        }
    }
}