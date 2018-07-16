using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.templates)]
    public sealed class GetTemplatesOperatorController : OperatorController
    {
        //Input keys

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("C2CA8DE2-5320-4788-BF81-96853CBD61B8", "Results");

        public GetTemplatesOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public GetTemplatesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>();

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("6CD6E948-800D-4536-9985-154D7A0347DC", "Access Template List");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ResultsKey] = MainPage.Instance.MainDocument.GetField<ListController<DocumentController>>(KeyStore.TemplateListKey);
        }

        public override FieldControllerBase GetDefaultController() => new TemplateAssignmentOperatorController();
    }
}