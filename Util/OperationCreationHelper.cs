using Dash.Controllers.Operators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static class OperationCreationHelper
    {

        public static ObservableDictionary<string, OperatorBuilder> Operators { get; } = new ObservableDictionary<string, OperatorBuilder>();

        static OperationCreationHelper()
        {
            AddOperator(() => new AddOperatorFieldModelController(), "Add");
            AddOperator(() => new SubtractOperatorFieldModelController(), "Subtract");
            AddOperator(() => new MultiplyOperatorFieldModelController(), "Multiply");
            AddOperator(() => new DivideOperatorFieldModelController(), "Divide");
            AddOperator(() => new IntersectionOperatorModelController(), "Intersection");
            AddOperator(() => new UnionOperatorFieldModelController(), "Union");
            AddOperator(() => new ZipOperatorFieldController(), "Zip");
            AddOperator(() => new ConcatOperator(), "Concat");
            AddOperator(() => new DocumentAppendOperatorController(), "Append");
            AddOperator(() => new ImageOperatorFieldModelController(), "UriToImage");
            AddOperator(() => new FilterOperatorFieldModelController(), "Filter", rfmc => new FilterOperatorBox(rfmc));
            AddOperator(() => new ApiOperatorController(), "Api", rfmc => new ApiOperatorBox(rfmc));
            AddOperator(() => new CollectionMapOperator(), "Map", rfmc => new CollectionMapOperatorBox(rfmc));
            AddOperator(() => new CompoundOperatorFieldController(), "Compound");
            AddOperator(() => new MeltOperatorFieldModelController(), "Melt", rfmc => new MeltOperatorBox(rfmc));
            AddOperator(() => new ExtractSentencesOperatorFieldModelController(), "Extract Sentences", rfmc => new ExtractSentencesOperatorBox(rfmc));
            AddOperator(() => new ExtractKeywordsOperatorFieldModelController(), "Extract KeyWords");

            //TODO fix DB special case
            //AddOperator<DBFilterOperatorFieldModelController>("DBFilter", OperatorDocumentFactory.CreateDBFilterDocumentController, "⊇");

        }

        public static void AddOperator(Func<OperatorFieldModelController> op, string title, Func<ReferenceFieldModelController, CourtesyDocument> layoutFunc = null)
        {
            Operators[title] = new OperatorBuilder(() => OperatorDocumentFactory.CreateOperatorDocument(op(), title, layoutFunc), op, title);
        }


        // TODO fix DB special case
        public static void AddOperator<T>(string name, Func<DocumentController> docGeneratorFunc, string icon) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, () => new T(), name);
        }

        public static OperatorFieldModelController GetOperatorController(string operatorType)
        {
            OperatorBuilder builder = null;
            Operators.TryGetValue(operatorType, out builder);
            return builder?.OperationControllerConstructor();
        }
    }
}
