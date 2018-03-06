﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static readonly KeyController TermKey = new KeyController("CC94FF84-09E5-4DB8-B962-C5ECF8EE9AE9", "Answer");
        public static readonly KeyController ImageKey = new KeyController("B3A5C8E3-6A24-4E4C-BAAE-BE1F9A7BE7C4", "Image Prompt");
        public static readonly KeyController CollectionKey = new KeyController("B6EC859F-027C-46A2-A569-DFC59F0913D8", "Collection");
        public static readonly KeyController TitleKey = new KeyController("326B801F-3E84-4BD7-890D-FEFD8B92ADC7", "Quiz Title");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(TermKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ImageKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(CollectionKey, new IOInfo(TypeInfo.List, true)),
        };


        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {

        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            return "QuizletOperator()";
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new QuizletOperator();
        }
    }
}
