﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.find, Op.Name.f/*, Op.Name.search*/)]
    public sealed class SimplifiedSearchOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController QueryKey = KeyController.Get("Query");

        //Output keys
        public static readonly KeyController ResultsKey = KeyController.Get("Results");

        public SimplifiedSearchOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public SimplifiedSearchOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override FieldControllerBase GetDefaultController() => new SimplifiedSearchOperatorController();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(QueryKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.List
        };

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("Simple Search");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO not have the function calls hardcoded here as strings.  We should find a dynamic way to reference Dish script function string names
            var searchQuery = (inputs[QueryKey] as TextController)?.Data ?? "";
            var results = new ListController<FieldControllerBase>();
            results.AddRange(Search.Parse(searchQuery).Select(res => res.ViewDocument).ToList());
            outputs[ResultsKey] = results;
            return Task.CompletedTask;
        }
    }
}
