﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DashShared;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist;
using Gma.CodeCloud.Controls.TextAnalyses.Extractors;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using NewControls;
using TagCloud;

namespace Dash
{
    public class ExtractKeywordsOperatorController : OperatorController
    {

        // Input Keys
        public static readonly KeyController InputCollection = new KeyController("F04A6C53-4C60-48F6-9B22-9084E1BA0D25", "Input Collection");
        public static readonly KeyController TextField = new KeyController("F0370FFD-C15E-41D2-BB4F-353F948E4FF8", "Text Field");

        // Output Keys
        public static readonly KeyController OutputCollection = new KeyController("142ED4F4-56AE-4481-8566-BD207A3586BB", "Output");

        // Helper Key
        public static readonly KeyController KeyWords = new KeyController("D4E89394-35EA-477B-959E-1A96F5CC2D39", "KeyWords");


        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } =
            new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
            {
                new KeyValuePair<KeyController, IOInfo>(InputCollection, new IOInfo(TypeInfo.List, true)),
                new KeyValuePair<KeyController, IOInfo>(TextField, new IOInfo(TypeInfo.Text, true)),
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputCollection] = TypeInfo.List
            };

        public ExtractKeywordsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ExtractKeywordsOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("8EA60017-CF8E-4885-B712-7C38906C299F", "Keywords");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, Scope scope = null)
        {
            var collection = inputs[InputCollection] as ListController<DocumentController>;
            var textFieldKeyId = (inputs[TextField] as TextController).Data;
            var textFieldKey = ContentController<FieldModel>.GetController<KeyController>(textFieldKeyId);

            // get all the text from the input documents
            var allText = "";
            // for each doc in the input collection
            foreach (var inputDoc in collection.TypedData)
            {
                // get the data from it if it exists
                var dataDoc = inputDoc.GetDataDocument();
                // get the text and add it to allText if the text exists
                var textInput = dataDoc.GetField(textFieldKey) as TextController;
                if (textInput != null)
                {
                    allText = allText + textInput.Data;
                }
            }

            // get all the keywords using some black box methods
            var blacklist = ComponentFactory.CreateBlacklist(true);
            var customBlacklist = CommonBlacklist.CreateFromTextFile("");
            var terms = new StringExtractor(allText, new NullProgressIndicator());
            var stemmer = ComponentFactory.CreateWordStemmer(true);
            var words = terms.Filter(blacklist).Filter(customBlacklist).CountOccurences();
            var keyWords = words.GroupByStem(stemmer).SortByOccurences().Cast<IWord>().ToList();

            var outputDocs = new List<DocumentController>();
            foreach (var inputDoc in collection.TypedData)
            {
                var dataDoc = inputDoc.GetDataDocument();
                var textInput = dataDoc.GetField(textFieldKey) as TextController;
                if (textInput != null)
                {
                    var presentKeyWords = keyWords.Where(kw => textInput.Data.Contains(kw.Text));
                    var outputDoc = dataDoc.MakeDelegate();
                    var textControllers = presentKeyWords.SortByOccurences().Select(kw => new TextController(kw.Text));
                    outputDoc.SetField(KeyWords, new ListController<TextController>(textControllers), true);
                    outputDocs.Add(outputDoc);
                }

            }

            outputs[OutputCollection] = new ListController<DocumentController>(outputDocs);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ExtractKeywordsOperatorController();
        }
    }
}
