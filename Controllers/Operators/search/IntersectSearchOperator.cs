﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("intersectSearch")]
    public class IntersectSearchOperator : OperatorController
    {
        //Input keys
        public static readonly KeyController Dict1Key = new KeyController("4DD8F9C5-4266-4279-9D24-FD5AFBC44369", "Dict1");
        public static readonly KeyController Dict2Key = new KeyController("420EB524-8373-4144-9433-87C0AF6D6CA7", "Dict2");

        //Output keys
        public static readonly KeyController ResultsKey = new KeyController("8E3931E4-6332-4A52-85F2-EC79031CB520", "DictionaryResults");

        public IntersectSearchOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }
        public IntersectSearchOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("835EDA32-5D1A-4C4B-B597-B664EC83C348", "Intersect Search");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(Dict1Key, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(Dict2Key, new IOInfo(TypeInfo.Document, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultsKey] = TypeInfo.Document
        };

        /// <summary>
        /// Compares two dictionaries that are obtained by searching the two terms individually in 
        /// PutSearchResultsIntoDictionaryOperator. Once that is done, both dictionaries are compares for similiarities,
        /// which are put into a new dictionary.
        /// </summary>
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, Scope scope = null)
        {
            var d1 = inputs[Dict1Key] as DocumentController;
            var d2 = inputs[Dict2Key] as DocumentController;
            
            var d3 = new DocumentController();
            foreach (var kvp in d1.EnumFields())
            {
                var l1 = kvp.Value as ListController<DocumentController>;
                var l2 = d2.GetField<ListController<DocumentController>>(kvp.Key);

                if (l1 != null && l2 != null)
                {
                    d3.SetField(kvp.Key, new ListController<DocumentController>(l1.TypedData.Concat(l2.TypedData)), true);
                }
            }

            outputs[ResultsKey] = d3;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IntersectSearchOperator();
        }
    }
}
