﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.to_list)]
    public class ToListOperatorController : OperatorController
    {
        public ToListOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public ToListOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Create a list from a single number");

        //Input keys
        public static readonly KeyController SourceKey = KeyController.Get("Source content");

        //Output keys
        public static readonly KeyController ComputedResultKey = KeyController.Get("Computed Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(SourceKey, new IOInfo(TypeInfo.Any, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo> { [ComputedResultKey] = TypeInfo.Number };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var source = inputs[SourceKey];
            IListController newList = null;

            //TODO Use operator overloading instead of typeswitching
            switch (source.TypeInfo)
            {
                case TypeInfo.Text:
                    newList = new ListController<TextController>();
                    var sourceText = ((TextController) source).Data.ToCharArray();
                    foreach (var c in sourceText) { ((ListController<TextController>)newList).Add(new TextController(c.ToString())); }
                    break;
                case TypeInfo.Number:
                    newList = new ListController<NumberController>();
                    var sourceNum = ((NumberController)source).Data.ToString("G").ToCharArray();
                    foreach (var n in sourceNum)
                    {
                        double.TryParse(n.ToString(), out var num);
                        ((ListController<NumberController>)newList).Add(new NumberController(num));
                    }
                    break;
            }

            outputs[ComputedResultKey] = newList?.AsField() ?? throw new ScriptExecutionException(new InvalidListCreationErrorModel(source.TypeInfo));
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ToListOperatorController();
    }
}
