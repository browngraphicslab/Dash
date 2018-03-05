using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;
using Dash.Controllers.Operators;

namespace Dash
{
    public class ExtractSentencesOperatorController : OperatorController
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
                [InputCollection] = new IOInfo(TypeInfo.List, true),
                [TextField] = new IOInfo(TypeInfo.Text, true)
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>()
            {
                [OutputCollection] = TypeInfo.List
            };

        public ExtractSentencesOperatorController() : base(new OperatorModel(OperatorType.SentenceAnalyzer))
        {
        }

        public ExtractSentencesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var collection = inputs[InputCollection] as ListController<DocumentController>;
            var textFieldKeyId = (inputs[TextField] as TextController).Data;
            var textFieldKey = ContentController<FieldModel>.GetController<KeyController>(textFieldKeyId);

            var outputDocs = new List<DocumentController>();
            foreach (var inputDoc in collection.TypedData)
            {
                var dataDoc = inputDoc.GetDataDocument(null);
                var textInput = (dataDoc.GetDereferencedField(textFieldKey,null) as TextController)?.Data;
                if (textInput != null)
                {
                    var sentences = Regex.Split(textInput, @"(?<=[\.!\?])\s+");


                    //var sentenceIndex = 0;
                    foreach (var sentence in sentences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList())
                    {
                        var outputDoc = dataDoc.MakeDelegate();
                        outputDoc.SetField(SentenceKey, new RichTextController(new RichTextModel.RTD(sentence)), true);
                        outputDoc.SetField(SentenceLengthKey, new NumberController(sentence.Length), true);
                        outputDoc.SetField(SentenceScoreKey, new NumberController((int) (new Random().NextDouble() * 100)), true);

                        var docLayout = new RichTextBox(new DocumentReferenceController(dataDoc.GetId(), SentenceKey), 0, 0, 200, 200).Document;
                        docLayout.SetField(KeyStore.DocumentContextKey, outputDoc, true);
                        outputDocs.Add(docLayout);


                        //sentenceIndex += sentence.Length;
                    }

                }
            }

            outputs[OutputCollection] = new ListController<DocumentController>(outputDocs);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ExtractSentencesOperatorController();
        }

    }
}
