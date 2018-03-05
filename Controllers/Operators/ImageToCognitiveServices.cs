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
    /// <summary>
    /// OperatorFieldModelController for an image recognition operator
    /// </summary>
    public class ImageToCognitiveServices : OperatorController
    {
        public ImageToCognitiveServices(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        public ImageToCognitiveServices() : base(new OperatorModel(OperatorType.ImageRecognition))
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

        /// <summary>
        /// Uses the ComputerVision helper class to call the ProjectOxford API for computer vision on inputs.
        /// Output string is the aggregate of all tags.
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var tags = "";
            var value = inputs.ElementAt(0).Value;
            var controller = value as ImageController;
            if (controller != null)
            {
                AnalysisResult result = null;
                try
                {
                    result = Task
                        .Run(() => ComputerVision.UploadAndAnalyzeImage(controller.ImageFieldModel.Data.LocalPath))
                        .Result;
                }
                catch
                {
                    result = Task.Run(() => ComputerVision.AnalyzeUrl(controller.ImageFieldModel.Data.AbsoluteUri)).Result;
                }
                if (result == null)
                    return;
                var allTags = result.Tags.Select(tag => tag.Name);
                tags = allTags.Aggregate(tags, (current, tag) => current + tag + ", ");
            }
            tags = tags.TrimEnd(' ').TrimEnd(',');
            outputs[DescriptorKey] = new TextController(tags);
        }

        /// <summary>
        /// Copies this operator
        /// </summary>
        /// <returns>A copy of this operator.</returns>
        public override FieldControllerBase GetDefaultController()
        {
            return new ImageToCognitiveServices(OperatorFieldModel);
        }
    }
}
