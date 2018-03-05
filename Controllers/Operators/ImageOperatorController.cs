﻿using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class ImageOperatorController : OperatorController
    {
        public ImageOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public ImageOperatorController( ) : base(new OperatorModel(OperatorType.ImageToUri))
        {
        }

        public static readonly KeyController URIKey = new KeyController("A6D348D8-896B-4726-A2F9-EF1E8F1690C9", "URI");

        public static readonly KeyController ImageKey = new KeyController("5FD13EB5-E5B1-4904-A611-599E7D2589AF", "Image");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [URIKey] = new IOInfo(TypeInfo.Text, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ImageKey] = TypeInfo.Image
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {

        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ImageOperatorController(OperatorFieldModel);
        }
    }
}
