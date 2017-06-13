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
    public class SubtractFieldModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("D75D911C-81EE-48F4-9733-BD8391B1725C", "Input A");
        public static readonly Key BKey = new Key("DB329759-200E-412C-815A-B70EFC70C6ED", "Input B");

        //Output keys
        public static readonly Key DifferenceKey = new Key("A57C588B-5F3B-4504-B1F1-374647B7BC12", "Difference");

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
            result[DifferenceKey] = new NumberFieldModel(a - b);
            return result;
        }

        public override UIElement MakeView(TemplateModel template)
        {
            Rectangle rect = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.OrangeRed)
            };

            return rect;
        }
    }
}
