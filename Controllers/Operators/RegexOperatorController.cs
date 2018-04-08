﻿using System.Collections.Generic;
using DashShared;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dash
{
    public class RegexOperatorController : OperatorController
    {
        public RegexOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public RegexOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("434B2CBC-003A-4DAD-8E8B-7F759A39B37C", "Regex");

        //Input keys
        public static readonly KeyController ExpressionKey      = new KeyController("0FA9226F-35BB-4AEE-A830-C81FF9611F3E", "Expression");
        public static readonly KeyController SplitExpressionKey = new KeyController("1AB31BED-2FF8-4C84-96E1-7B3C739038AC", "SplitExpression");
        public static readonly KeyController TextKey            = new KeyController("B4D356C5-361E-4538-BB4D-F14C85159312", "Text");

        //Output keys
        public static readonly KeyController MatchesKey = new KeyController("9C395B1C-A7A7-47A4-9F30-3B83CD2D0939", "Matches");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ExpressionKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(SplitExpressionKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [MatchesKey] = TypeInfo.List,
        };

        static DocumentController _prototype = null;
        
        void initProto()
        {
            if (_prototype == null)
            {
                _prototype = new DocumentController(new Dictionary<KeyController, FieldControllerBase>(),
                    new DocumentType(UtilShared.GetDeterministicGuid("RegexOutput"), "RegexOutput"));
                _prototype.SetField(KeyStore.AbstractInterfaceKey, new TextController(_prototype.DocumentType.Type + "API"), true);
            }
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            initProto();
            var text = (inputs[TextKey] is ListController<TextController>) ? (inputs[TextKey] as ListController<TextController>).Data.Aggregate("", (init, fm) => init + " " + (fm as TextController).Data ) :
                (inputs[TextKey] as TextController).Data;
            var expr = (inputs[ExpressionKey] as TextController).Data;
            var split = (inputs[SplitExpressionKey] as TextController).Data;
            var rsplit = new Regex(split);
            var ematch = new Regex(expr);
            var splits = rsplit.Split(text);

            var collected = new List<TextController>();
            foreach (var s in splits)
                if (ematch.IsMatch(s))
                {
                    collected.Add(new TextController(s));
                }
            outputs[MatchesKey] = new ListController<TextController>(collected);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new RegexOperatorController(OperatorFieldModel);
        }
    }
}