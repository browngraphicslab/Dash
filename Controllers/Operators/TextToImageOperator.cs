using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType("textToImage")]

    public class TextToImageOperator : OperatorController
    {

        public TextToImageOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public TextToImageOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey =
            new KeyController("Text To Image", "5DF53FC2-1ADC-446E-98AE-D7F8764C0FA1");

        //Input keys
        public static readonly KeyController TextKey = new KeyController("Text");

        //Output keys
        public static readonly KeyController ImageKey = new KeyController("Image");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
            {
                new KeyValuePair<KeyController, IOInfo>(TextKey, new IOInfo(TypeInfo.Text, true))
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [ImageKey] = TypeInfo.Image,
            };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var textController = inputs[TextKey] as TextController;
            var uri = textController.Data;

            try
            {
                outputs[ImageKey] = new ImageController(new Uri(uri));
            }
            catch (Exception e)
            {
                return;
            }   
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new TextToImageOperator();
        }
    }

}
