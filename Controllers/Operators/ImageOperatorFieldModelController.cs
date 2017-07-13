using System;
using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class ImageOperatorFieldModelController : OperatorFieldModelController
    {
        public ImageOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public static readonly Key URIKey = new Key("A6D348D8-896B-4726-A2F9-EF1E8F1690C9", "URI");

        public static readonly Key ImageKey = new Key("5FD13EB5-E5B1-4904-A611-599E7D2589AF", "Image");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [URIKey] = TypeInfo.Text
        };

        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [ImageKey] = TypeInfo.Image
        };

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            throw new NotImplementedException();

            //TextFieldModel uri = doc.Field(URIKey) as TextFieldModel;
            //Debug.Assert(uri != null, "Input is not a string");

            //(doc.Field(ImageKey) as ImageFieldModel).Data = new BitmapImage(new Uri(uri.Data));
        }
    }
    /* 
    public class ImageOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key URIKey = new Key("A6D348D8-896B-4726-A2F9-EF1E8F1690C9", "URI");

        //Output keys
        public static readonly Key ImageKey = new Key("5FD13EB5-E5B1-4904-A611-599E7D2589AF", "Image");

        public override List<Key> InputKeys { get; } = new List<Key> {URIKey};

        public override List<Key> OutputKeys { get; } = new List<Key> {ImageKey};

        public override List<FieldModel> GetNewInputFields()
        {
            return new List<FieldModel>
            {
                new TextFieldModel("Uri")
            };
        }

        public override List<FieldModel> GetNewOutputFields()
        {
            return new List<FieldModel>
            {
                new ImageFieldModel(new Uri("ms-appx://Dash/Assets/cat2.jpeg"))
            };
        }

        public override void Execute(DocumentModel doc)
        {
            throw new NotImplementedException();

            //TextFieldModel uri = doc.Field(URIKey) as TextFieldModel;
            //Debug.Assert(uri != null, "Input is not a string");

            //(doc.Field(ImageKey) as ImageFieldModel).Data = new BitmapImage(new Uri(uri.Data));
        }
    }
    */
}
