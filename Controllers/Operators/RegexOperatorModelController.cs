using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Dash
{
    public class RegexOperatorFieldModelController : OperatorFieldModelController
    {
        public RegexOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public RegexOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.Regex))
        {
        }

        //Input keys
        public static readonly KeyController ExpressionKey      = new KeyController("0FA9226F-35BB-4AEE-A830-C81FF9611F3E", "Expression");
        public static readonly KeyController SplitExpressionKey = new KeyController("1AB31BED-2FF8-4C84-96E1-7B3C739038AC", "SplitExpression");
        public static readonly KeyController TextKey            = new KeyController("B4D356C5-361E-4538-BB4D-F14C85159312", "Text");

        //Output keys
        public static readonly KeyController MatchesKey = new KeyController("9C395B1C-A7A7-47A4-9F30-3B83CD2D0939", "Matches");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [TextKey]            = new IOInfo(TypeInfo.Text, true),
            [ExpressionKey]      = new IOInfo(TypeInfo.Text, true),
            [SplitExpressionKey] = new IOInfo(TypeInfo.Text, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [MatchesKey] = TypeInfo.Collection,
        };

        static DocumentController _prototype = null;
        
        void initProto()
        {
            if (_prototype == null)
            {
                _prototype = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
                    new DocumentType(DashShared.Util.GetDeterministicGuid("RegexOutput"), "RegexOutput"));
                _prototype.SetField(KeyStore.AbstractInterfaceKey, new TextFieldModelController(_prototype.DocumentType.Type + "API"), true);
            }
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            initProto();
            var text = (inputs[TextKey] is ListFieldModelController<TextFieldModelController>) ? (inputs[TextKey] as ListFieldModelController<TextFieldModelController>).Data.Aggregate("", (init, fm) => init + " " + (fm as TextFieldModelController).Data ) :
                (inputs[TextKey] as TextFieldModelController).Data;
            var expr = (inputs[ExpressionKey] as TextFieldModelController).Data;
            var split = (inputs[SplitExpressionKey] as TextFieldModelController).Data;
            var rsplit = new Regex(split);
            var ematch = new Regex(expr);
            var splits = rsplit.Split(text);

            var collected = new List<TextFieldModelController>();
            foreach (var s in splits)
                if (ematch.IsMatch(s))
                {
                    collected.Add(new TextFieldModelController(s));
                }
            outputs[MatchesKey] = new ListFieldModelController<TextFieldModelController>(collected);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new AddOperatorFieldModelController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}