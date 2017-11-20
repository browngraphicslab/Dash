using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.ProjectOxford.Vision.Contract;

namespace Dash.Controllers.Operators
{
    public class ImageRecognitionOperatorFieldModelController : OperatorFieldModelController
    {
        public ImageRecognitionOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public ImageRecognitionOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.ImageRecognition))
        {
        }

        public static readonly KeyController ImageKey = new KeyController("2HGGH89D-SH43-SDGF-25HD-DAFI9E8HF8HF", "Image");

        public static readonly KeyController DescriptorKey = new KeyController("HL3H9R8K-634H-FDHG-4HWH-RG5IORGPHS33", "Descriptor");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [ImageKey] = new IOInfo(TypeInfo.Image, true)
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DescriptorKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var tags = "";
            var value = inputs.ElementAt(0).Value;
            var controller = value as ImageFieldModelController;
            if (controller != null)
            {
                var result = Task.Run(() => ComputerVision.UploadAndAnalyzeImage(controller.ImageFieldModel.Data.AbsolutePath)).Result;
                var allTags = result.Tags.Select(tag => tag.Name);
                tags = allTags.Aggregate(tags, (current, tag) => current + tag + ", ");
            }
            tags = tags.TrimEnd(' ').TrimEnd(',');
            outputs[DescriptorKey] = new TextFieldModelController(tags);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new ImageRecognitionOperatorFieldModelController(OperatorFieldModel);
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
