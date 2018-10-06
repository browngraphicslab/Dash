using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents before a given time
    /// </summary>
    [OperatorType(Op.Name.alias)]
    public class GetAllDocsByAlias : OperatorController
    {
        //Input keys
        public static readonly KeyController IdKey = new KeyController("Id");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public GetAllDocsByAlias() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }
        public GetAllDocsByAlias(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(IdKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, DashShared.TypeInfo>()
            {
                [ResultsKey] = TypeInfo.List
            };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Alias", "DB543B75-15D3-467A-A9DE-9F262F496C25");

        /// <inheritdoc />
        /// <summary>
        /// Searches through all documents in the dash view and compares their data documents to find aliases
        /// </summary>
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var toReturn = new ListController<DocumentController>();
            var id = (inputs[IdKey] as TextController)?.Data;

            if (!string.IsNullOrEmpty(id))
            {
                toReturn.AddRange(Search.SearchByAlias(id).Select(res => res.ViewDocument).ToList());
            }

            outputs[ResultsKey] = toReturn;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new GetAllDocsByAlias();
    }
}
