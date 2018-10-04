using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public ImageToCognitiveServices() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Image Cog Services");

        public static readonly KeyController ImageKey = new KeyController("Image");
        public static readonly KeyController DescriptorKey = new KeyController("Descriptor");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(ImageKey, new IOInfo(TypeInfo.Image, true))
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
        /// <param name="args"></param>
        /// <param name="scope"></param>
        /// <param name="state"></param>
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
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
                    return Task.CompletedTask;
                var allTags = result.Tags.Select(tag => tag.Name);
                tags = allTags.Aggregate(tags, (current, tag) => current + tag + ", ");
            }
            tags = tags.TrimEnd(' ').TrimEnd(',');
            outputs[DescriptorKey] = new TextController(tags);
            return Task.CompletedTask;
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
