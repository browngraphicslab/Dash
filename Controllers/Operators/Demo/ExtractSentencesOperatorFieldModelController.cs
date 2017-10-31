using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;
using Dash.Controllers.Operators;

namespace Dash
{
    public class ExtractSentencesOperatorFieldModelController : OperatorFieldModelController
    {
        // Input Keys
        public static readonly KeyController InputCollection = new KeyController("00EABCCE-3CDC-4FD8-A419-2571EC4D0439", "Input Collection");
        public static readonly KeyController TextField = new KeyController("87C2116A-9853-4884-BDCD-E3F5124F687E", "Text Field");

        // Output Keys
        public static readonly KeyController OutputCollection = new KeyController("D947CBFD-5EF7-4503-A2B4-8CB42A2B2901", "Output");

        // helper key to store sentences in the output
        public static readonly KeyController SentenceKey = new KeyController("528F8275-99FD-48A3-8B9D-71CEB0856078", "Sentence");

        // helper key to store sentences in the output
        public static readonly KeyController IndexKey = new KeyController("4e852433-b6cc-43f2-a37c-636e1e61cd8b", "Index");
        public static readonly KeyController SentenceLengthKey = new KeyController("D668A5C4-C41B-4802-B9B1-918C40D3012E", "Sentence Length");
        public static readonly KeyController SentenceScoreKey = new KeyController("C20594BD-C087-483B-9A35-E450EE36DFE1", "Sentence Score");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>()
            {
                [InputCollection] = new IOInfo(TypeInfo.Collection, true),
                [TextField] = new IOInfo(TypeInfo.Text, true)
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>()
            {
                [OutputCollection] = TypeInfo.Collection
            };

        public ExtractSentencesOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.ExtractSentences))
        {
        }

        public ExtractSentencesOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var collection = inputs[InputCollection] as DocumentCollectionFieldModelController;
            var textFieldKeyId = (inputs[TextField] as TextFieldModelController).Data;
            var textFieldKey = ContentController<KeyModel>.GetController<KeyController>(textFieldKeyId);

            var outputDocs = new List<DocumentController>();
            foreach (var inputDoc in collection.Data)
            {
                var dataDoc = Util.GetDataDoc(inputDoc, null);
                var textInput = (dataDoc.GetDereferencedField(textFieldKey,null) as TextFieldModelController)?.Data ??
                                (dataDoc.GetDereferencedField(textFieldKey, null) as RichTextFieldModelController)?.Data?.ReadableString;
                if (textInput != null)
                {
                    var sentences = Regex.Split(textInput, @"(?<=[\.!\?])\s+");

                    var protoLayout = new RichTextBox(new DocumentReferenceFieldController(dataDoc.GetId(), SentenceKey), 0, 0, double.NaN, double.NaN).Document;

                    //var sentenceIndex = 0;
                    foreach (var sentence in sentences.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        var outputDoc = dataDoc.MakeDelegate();
                        outputDoc.SetField(SentenceKey, new RichTextFieldModelController(new RichTextFieldModel.RTD(sentence)), true);
                        outputDoc.SetField(SentenceLengthKey, new NumberFieldModelController(sentence.Length), true);
                        outputDoc.SetField(SentenceScoreKey, new NumberFieldModelController((int) new Random().NextDouble() * 100), true);

                        var docLayout = protoLayout.MakeDelegate();
                        docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(outputDoc), true);
                        outputDocs.Add(docLayout);


                        //sentenceIndex += sentence.Length;
                    }

                }
            }

            outputs[OutputCollection] = new DocumentCollectionFieldModelController(outputDocs);
        }





        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new ExtractSentencesOperatorFieldModelController();
        }

        public override bool SetValue(object value)
        {
            return false;
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

    }
}
