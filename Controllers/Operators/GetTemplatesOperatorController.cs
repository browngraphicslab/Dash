using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.templates)]
    public sealed class GetTemplatesOperatorController : OperatorController
    {
        //Input keys

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public GetTemplatesOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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
        private static readonly KeyController TypeKey = KeyController.Get("Access Template List");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ResultsKey] = MainPage.Instance.MainDocument.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.TemplateListKey);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new TemplateAssignmentOperatorController();
    }
}
