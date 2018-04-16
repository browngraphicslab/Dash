using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("texttoVideo")]
    class TextToVideoOperatorController : OperatorController
    {
        public TextToVideoOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        { }

        public TextToVideoOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        { }

        public static readonly KeyController URIKey = new KeyController("A6D348D8-896B-4726-A2F9-EF1E8F1690C9", "URI");

        public static readonly KeyController VideoKey = new KeyController("E50DDE44-326F-4709-BCA8-C44E5FA7CB53", "Video");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(URIKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [VideoKey] = TypeInfo.Video
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("379173B5-3E72-44E8-8EA0-D5EA946CA173", "Text to Video", true);

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var uriController = inputs[URIKey] as TextController;
            if (uriController != null)
            {
                try {
                    outputs[VideoKey] = new VideoController(new Uri(uriController.Data));
                }
                catch (UriFormatException e)
                {
                    return;
                }


            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new TextToVideoOperatorController();
        }
    }
}
