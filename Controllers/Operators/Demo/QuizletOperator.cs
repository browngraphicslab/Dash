using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class QuizletOperator : OperatorController
    {
        public QuizletOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public QuizletOperator() : base(new OperatorModel(OperatorType.Quizlet))
        {
        }

        //Input keys
        public static readonly KeyController TermKey = new KeyController("CC94FF84-09E5-4DB8-B962-C5ECF8EE9AE9", "Term");
        public static readonly KeyController ImageKey = new KeyController("B3A5C8E3-6A24-4E4C-BAAE-BE1F9A7BE7C4", "Image");
        public static readonly KeyController DefinitionKey = new KeyController("F5417EB3-FEDE-41E9-8B17-BDDD8E4A5AC8", "Definition");
        public static readonly KeyController CollectionKey = new KeyController("B6EC859F-027C-46A2-A569-DFC59F0913D8", "Collection");
        public static readonly KeyController TitleKey = new KeyController("326B801F-3E84-4BD7-890D-FEFD8B92ADC7", "Quiz Title");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [TermKey] = new IOInfo(TypeInfo.Text, true),
            [DefinitionKey] = new IOInfo(TypeInfo.Text, false),
            [ImageKey] = new IOInfo(TypeInfo.Text, true),
            [CollectionKey] = new IOInfo(TypeInfo.List, true),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {

        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new QuizletOperator();
        }
    }
}
