using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{

    /// <summary>
    /// this class represents a fake operator controller.  
    /// This was made so that the language can parse the 'let' operation just like any operator, but can override its functionality
    /// </summary>
    [OperatorType("let")]
    public class LetOperatorController : OperatorController
    {
        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("ED03AF63-0A70-4EC5-BB3B-3F9DF621C1A1", "Let");

        //Input keys
        public static readonly KeyController VariableNameKey =
            new KeyController("A35F1E4F-B876-428C-BB21-EB0A33422118", "Variable");

        public static readonly KeyController VariableValueKey =
            new KeyController("8F6CE1B3-0D2A-4A62-8BB2-F848A7BB808B", "Value");

        public static readonly KeyController ContinuedExpressionKey =
            new KeyController("52CE71E6-14D7-4606-A235-B0A9B5880B27", "Expression");

        //Output keys
        public static readonly KeyController ReturnValueKey =
            new KeyController("D19EC771-E30F-477D-9C14-31CA33F04491", "ReturnValue");

        public LetOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public LetOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            throw new NotImplementedException();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
            {
                new KeyValuePair<KeyController, IOInfo>(VariableNameKey, new IOInfo(DashShared.TypeInfo.Any, true)),
                new KeyValuePair<KeyController, IOInfo>(VariableValueKey, new IOInfo(DashShared.TypeInfo.Any, true)),
                new KeyValuePair<KeyController, IOInfo>(ContinuedExpressionKey,
                    new IOInfo(DashShared.TypeInfo.Any, true)),
            };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, DashShared.TypeInfo>()
            {
                [ReturnValueKey] = DashShared.TypeInfo.Any
            };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args,
            ScriptState state = null)
        {
            throw new NotImplementedException();
        }
    }
}
