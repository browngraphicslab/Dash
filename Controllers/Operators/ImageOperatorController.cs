﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ImageOperatorController : OperatorController
    {
        public ImageOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public ImageOperatorController( ) : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Image Operator", "58032D3F-2B09-475B-8A56-F455DC809229");


        public static readonly KeyController URIKey = new KeyController("URI");

        public static readonly KeyController ImageKey = new KeyController("Image");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(URIKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ImageKey] = TypeInfo.Image
        };
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ImageOperatorController(OperatorFieldModel);
        }
    }
}
