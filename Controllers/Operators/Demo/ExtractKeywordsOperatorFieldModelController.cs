using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DashShared;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist;
using Gma.CodeCloud.Controls.TextAnalyses.Extractors;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using NewControls;
using TagCloud;

namespace Dash
{
    public class ExtractKeywordsOperatorFieldModelController : OperatorFieldModelController
    {

        // Input Keys
        public static readonly KeyController InputCollection = new KeyController("F04A6C53-4C60-48F6-9B22-9084E1BA0D25", "Input Collection");
        public static readonly KeyController TextField = new KeyController("F0370FFD-C15E-41D2-BB4F-353F948E4FF8", "Text Field");

        // Output Keys
        public static readonly KeyController OutputCollection = new KeyController("142ED4F4-56AE-4481-8566-BD207A3586BB", "Output");

        // Helper Key
        public static readonly KeyController KeyWords = new KeyController("D4E89394-35EA-477B-959E-1A96F5CC2D39", "KeyWords");


        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } =
            new ObservableDictionary<KeyController, IOInfo>
            {
                [InputCollection] = new IOInfo(TypeInfo.Collection, true),
                [TextField] = new IOInfo(TypeInfo.Text, true)
            };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } =
            new ObservableDictionary<KeyController, TypeInfo>
            {
                [OutputCollection] = TypeInfo.Collection
            };

        public ExtractKeywordsOperatorFieldModelController(OperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ExtractKeywordsOperatorFieldModelController() : base(new OperatorFieldModel(OperatorType.Extract_Keywords))
        {
        }

        public override void ExecuteAsync(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var collection = inputs[InputCollection] as DocumentCollectionFieldModelController;
            var textFieldKeyId = (inputs[TextField] as TextFieldModelController).Data;
            var textFieldKey = ContentController<KeyModel>.GetController<KeyController>(textFieldKeyId);

            // get all the text from the input documents
            var allText = "";
            // for each doc in the input collection
            foreach (var inputDoc in collection.Data)
            {
                // get the data from it if it exists
                var dataDoc = inputDoc.GetDataDocument(null);
                // get the text and add it to allText if the text exists
                var textInput = dataDoc.GetField(textFieldKey) as TextFieldModelController;
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
            foreach (var inputDoc in collection.Data)
            {
                var dataDoc = inputDoc.GetDataDocument(null);
                var textInput = dataDoc.GetField(textFieldKey) as TextFieldModelController;
                if (textInput != null)
                {
                    var presentKeyWords = keyWords.Where(kw => textInput.Data.Contains(kw.Text));
                    var outputDoc = dataDoc.MakeDelegate();
                    var textFieldModelControllers = presentKeyWords.SortByOccurences().Select(kw => new TextFieldModelController(kw.Text));
                    outputDoc.SetField(KeyWords, new ListFieldModelController<TextFieldModelController>(textFieldModelControllers), true);
                    outputDocs.Add(outputDoc);
                }

            }

            outputs[OutputCollection] = new DocumentCollectionFieldModelController(outputDocs);
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new ExtractKeywordsOperatorFieldModelController(new OperatorFieldModel(OperatorType.Extract_Keywords));
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
