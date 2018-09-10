using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{

    /// <summary>
    /// this class represents a fake operator controller.  
    /// This was made so that the language can parse the 'let' operation just like any operator, but can override its functionality
    /// </summary>
    [OperatorType(Op.Name.let)]
    public class LetOperatorController : OperatorController
    {
        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = new KeyController("Let", new Guid("ED03AF63-0A70-4EC5-BB3B-3F9DF621C1A1"));

        //Input keys
        public static readonly KeyController VariableNameKey = new KeyController("Variable");

        public static readonly KeyController VariableValueKey = new KeyController("Value");

        public static readonly KeyController ContinuedExpressionKey = new KeyController("Expression");

        //Output keys
        public static readonly KeyController ReturnValueKey = new KeyController("ReturnValue");

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
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args,
            Scope scope = null)
        {
            throw new NotImplementedException();
        }
    }
}
