using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Quizlet");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } = rfmc => new QuizletOperatorBox(rfmc);

        //Input keys
        public static readonly KeyController TermKey = KeyController.Get("Answer");
        public static readonly KeyController ImageKey = KeyController.Get("Image Prompt");
        public static readonly KeyController CollectionKey = KeyController.Get("Collection");
        public static readonly KeyController TitleKey = KeyController.Get("Quiz Title");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TermKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ImageKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(CollectionKey, new IOInfo(TypeInfo.List, true)),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {

            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new QuizletOperator();
        }
    }
}
