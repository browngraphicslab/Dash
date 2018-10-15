using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class ExtractSentencesOperatorController : OperatorController
    {
        // Input Keys
        public static readonly KeyController InputCollection = KeyController.Get("Input Collection");
        public static readonly KeyController TextField = KeyController.Get("Text Field");

        // Output Keys
        public static readonly KeyController OutputCollection = KeyController.Get("Output");

        // helper key to store sentences in the output
        public static readonly KeyController SentenceKey = KeyController.Get("Sentence");

        // helper key to store sentences in the output
        public static readonly KeyController IndexKey = KeyController.Get("Index");
        public static readonly KeyController SentenceLengthKey = KeyController.Get("Sentence Length");
        public static readonly KeyController SentenceScoreKey = KeyController.Get("Sentence Score");

        public override Func<ReferenceController, CourtesyDocument> LayoutFunc { get; } =  rfmc => new ExtractSentencesOperatorBox(rfmc);

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
            {
                new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, true)),
                new KeyValuePair<KeyController, IOInfo>(TextField, new IOInfo(TypeInfo.Key, true))
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>()
            {
                [OutputCollection] = TypeInfo.List
            };

        public ExtractSentencesOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public ExtractSentencesOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Sentence Analyzer", new Guid("D9EE3561-0A30-4DA9-B11A-859CABCF237B"));

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var collection = inputs[InputCollection] as ListController<DocumentController>;
            var textFieldKey = inputs[TextField] as KeyController;

            var outputDocs = new List<DocumentController>();
            foreach (var inputDoc in collection.TypedData)
            {
                var dataDoc = inputDoc.GetDataDocument();
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

                        var docLayout = new RichTextBox(new DocumentReferenceController(dataDoc, SentenceKey), 0, 0, 200, 200).Document;
                        docLayout.SetField(KeyStore.DocumentContextKey, outputDoc, true);
                        outputDocs.Add(docLayout);


                        //sentenceIndex += sentence.Length;
                    }

                }
            }

            outputs[OutputCollection] = new ListController<DocumentController>(outputDocs);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ExtractSentencesOperatorController();
        }

    }
}
