﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(URIKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ImageKey] = TypeInfo.Image
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {

        }
        
        public override FieldModelController<OperatorModel> Copy()
        {
            return new ImageOperatorController(OperatorFieldModel);
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}
