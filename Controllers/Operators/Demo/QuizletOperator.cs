using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public class QuizletOperator : OperatorController
    {
        public QuizletOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public QuizletOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Quizlet", "B1F174E7-64C3-4DB7-9D67-67F9DB24BB54");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new QuizletOperatorBox(rfmc);

        //Input keys
        public static readonly KeyController TermKey = new KeyController("Answer");
        public static readonly KeyController ImageKey = new KeyController("Image Prompt");
        public static readonly KeyController CollectionKey = new KeyController("Collection");
        public static readonly KeyController TitleKey = new KeyController("Quiz Title");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TermKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ImageKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(CollectionKey, new IOInfo(TypeInfo.List, true)),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {

        }

        public override FieldControllerBase GetDefaultController()
        {
            return new QuizletOperator();
        }
    }
}
