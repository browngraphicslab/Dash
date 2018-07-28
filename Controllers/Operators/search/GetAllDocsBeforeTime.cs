﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// operator to get all documents before a given time
    /// </summary>
    [OperatorType(Op.Name.before)]
    public class GetAllDocsBeforeTime : OperatorController
    {
        //Input keys
        public static readonly KeyController TimeKey = new KeyController("Time");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("Results");

        public GetAllDocsBeforeTime() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public GetAllDocsBeforeTime(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(TimeKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, DashShared.TypeInfo>()
            {
                [ResultsKey] = TypeInfo.List
            };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Before", "27B6978D-F053-480B-8B64-439D334E5C9E");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var toReturn = new ListController<DocumentController>();

            var time = (inputs[TimeKey] as TextController)?.Data?.ToLower();

            if (!DateTime.TryParse(time, out DateTime givenTime))
            {
                return;
            }

            if (!string.IsNullOrEmpty(time))
            {
                var allResults = DocumentTree.MainPageTree.GetAllNodes();
                var data = allResults.Select(node => node.ViewDocument).ToList();
                foreach (var t in data)
                {
                    var docTimeS = t.GetDataDocument().GetField<Controllers.DateTimeController>(KeyStore.DateModifiedKey)?.Data;

                    //return all docs before givenTime
                    if (docTimeS < givenTime)
                    {
                        toReturn.Add(t);
                    }
                }

            }

            outputs[ResultsKey] = toReturn;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetAllDocsBeforeTime();
        }
    }
}
