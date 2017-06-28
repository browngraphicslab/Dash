using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    /*
    public class SubtractFieldModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key AKey = new Key("D75D911C-81EE-48F4-9733-BD8391B1725C", "Input A");
        public static readonly Key BKey = new Key("DB329759-200E-412C-815A-B70EFC70C6ED", "Input B");

        //Output keys
        public static readonly Key DifferenceKey = new Key("A57C588B-5F3B-4504-B1F1-374647B7BC12", "Difference");

        public override List<Key> InputKeys { get; } = new List<Key> {AKey, BKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {DifferenceKey};

        public override List<FieldModel> GetNewInputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel(), new NumberFieldModel()
            };
        }

        public override List<FieldModel> GetNewOutputFields()
        {
            return new List<FieldModel>
            {
                new NumberFieldModel()
            };
        }

        public override void Execute(DocumentModel doc)
        {
            throw new NotImplementedException();

            //NumberFieldModel numberA = doc.Field(AKey) as NumberFieldModel;
            //Debug.Assert(numberA != null, "Input is not a number");

            //NumberFieldModel numberB = doc.Field(BKey) as NumberFieldModel;
            //Debug.Assert(numberB != null, "Input is not a number");

            //double a = numberA.Data;
            //double b = numberB.Data;
            //(doc.Field(DifferenceKey) as NumberFieldModel).Data = a - b;
        }
    }
    */
}
