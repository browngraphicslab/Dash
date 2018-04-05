using Dash.Controllers.Operators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static class OperationCreationHelper
    {

        public static ObservableDictionary<string, OperatorBuilder> Operators { get; } = new ObservableDictionary<string, OperatorBuilder>();

        static OperationCreationHelper()
        {
//<<<<<<< HEAD
//            AddOperator(() => new AddOperatorController(), "Add");
//            AddOperator(() => new SubtractOperatorController(), "Subtract");
//            AddOperator(() => new MultiplyOperatorController(), "Multiply");
//            AddOperator(() => new DivideOperatorController(), "Divide");
//            AddOperator(() => new IntersectionOperatorController(), "Intersection");
//            AddOperator(() => new UnionOperatorController(), "Union");
//            AddOperator(() => new ZipOperatorController(), "Zip");
//            AddOperator(() => new ConcatOperatorController(), "Concat");
//            AddOperator(() => new DocumentAppendOperatorController(), "Append");
//            AddOperator(() => new ImageOperatorController(), "UriToImage");
//            AddOperator(() => new ApiOperatorController(), "Api", rfmc => new ApiOperatorBox(rfmc));
//            AddOperator(() => new CompoundOperatorController(), "Compound");
//            AddOperator(() => new ExecuteHtmlJavaScriptController(), "HtmlScript", rfmc => new ExecuteHtmlOperatorBox(rfmc));
//            AddOperator(() => new MeltOperatorController(), "Melt", rfmc => new MeltOperatorBox(rfmc));
//            AddOperator(() => new ExtractSentencesOperatorController(), "Sentence Analyzer", rfmc => new ExtractSentencesOperatorBox(rfmc));
//            AddOperator(() => new ExtractKeywordsOperatorController(), "Extract KeyWords");
//            AddOperator(() => new ImageToCognitiveServices(), "ImageRecognition");
//            AddOperator(() => new SearchOperatorController(), "Search", rfmc => new SearchOperatorBox(rfmc));
//            AddOperator(() => new QuizletOperator(), "Quizlet", rfmc => new QuizletOperatorBox(rfmc));
//            AddOperator(() => new ExecDishOperatorController(), "Exec Operator");
//            AddOperator(() => new GetScriptValueAsStringOperatorController(), "Exec To String Operator");
//            AddOperator(() => new SimplifiedSearchOperatorController(), "Simplified Search");
//            AddOperator(() => new GetKeysOperatorController(), "Get Keys");

//            //TODO fix DB special case
//            //AddOperator<DBFilterOperatorController>("DBFilter", OperatorDocumentFactory.CreateDBFilterDocumentController, "⊇");

//        }

//        public static void AddOperator(Func<OperatorController> op, string title, Func<ReferenceController, CourtesyDocument> layoutFunc = null)
//        {
//            Operators[title] = new OperatorBuilder(() => OperatorDocumentFactory.CreateOperatorDocument(op(), title, layoutFunc), op, title);
//        }


//        // TODO fix DB special case
//        public static void AddOperator<T>(string name, Func<DocumentController> docGeneratorFunc, string icon) where T : OperatorController, new()
//        {
//            Operators[name] = new OperatorBuilder(docGeneratorFunc, () => new T(), name);
//=======
            var operatorTypes = typeof(OperatorController).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(OperatorController)));
            foreach (var operatorType in operatorTypes)
            {
                var title = ((KeyController) operatorType
                    .GetField("TypeKey", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null))?.KeyModel?.Name;

                Debug.Assert(title != null, $"The operator type {operatorType} does not have a static private field TypeKey");
                AddOperator(() => (OperatorController)Activator.CreateInstance(operatorType), title);
            }
//>>>>>>> 502f0ed395f3df6f22c888c05a508f63ed76a410
        }

        public static void AddOperator(Func<OperatorController> op, string title)
        {
            Operators[title] = new OperatorBuilder(() => OperatorDocumentFactory.CreateOperatorDocument(op(), title), op, title);
        }
    }
}
