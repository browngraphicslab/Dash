﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents with a given field and value of that field
    /// </summary>
    [OperatorType(Op.Name.key_field_query, Op.Name.kv)]
    public sealed class GetAllDocumentsWithKeyFieldValuesOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController KeyQueryKey = KeyController.Get("KeyQuery");
        public static readonly KeyController ValueQueryKey = KeyController.Get("ValueQuery");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public GetAllDocumentsWithKeyFieldValuesOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public GetAllDocumentsWithKeyFieldValuesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(KeyQueryKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ValueQueryKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
        new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Key Field Query");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyQuery = (inputs[KeyQueryKey] as TextController)?.Data;
            var toReturn = new ListController<DocumentController>();

            if (!string.IsNullOrEmpty(keyQuery))
            {
                var negateCategory = keyQuery.StartsWith('!');
                keyQuery = keyQuery.TrimStart('!');

                var valueQuery = (inputs[ValueQueryKey] as TextController)?.Data?.ToLower() ?? "";
                var finalResults = Search.SearchByKeyValuePair(keyQuery, valueQuery, negateCategory).Select(res => res.ViewDocument).ToList();

                //TODO FURTHER modify the helpful text of these docs so the text is more helpful

                toReturn.AddRange(finalResults);
            }
            outputs[ResultsKey] = toReturn;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new GetAllDocumentsWithKeyFieldValuesOperatorController();
    }
}
