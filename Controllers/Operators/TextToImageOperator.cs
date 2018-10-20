using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.image)]

    public class TextToImageOperator : OperatorController
    {

        public TextToImageOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public TextToImageOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override KeyController OperatorType { get; } = TypeKey;

        private static readonly KeyController TypeKey = KeyController.Get("Text To Image");


        //Input keys
        public static readonly KeyController TextKey = KeyController.Get("Text");

        //Output keys
        public static readonly KeyController ImageKey = KeyController.Get("Image");

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

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var textController = inputs[TextKey] as TextController;
            var uri = textController?.Data;

            try
            {
                outputs[ImageKey] = new ImageController(new Uri(uri));
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new ImageCreationFailureErrorModel(uri));
            }   
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new TextToImageOperator();
    }

}
