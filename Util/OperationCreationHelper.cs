﻿using Dash.Controllers.Operators;
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
            AddOperator(() => new AddOperatorController(), "Add");
            AddOperator(() => new SubtractOperatorController(), "Subtract");
            AddOperator(() => new MultiplyOperatorController(), "Multiply");
            AddOperator(() => new DivideOperatorController(), "Divide");
            AddOperator(() => new IntersectionOperatorController(), "Intersection");
            AddOperator(() => new UnionOperatorController(), "Union");
            AddOperator(() => new ZipOperatorController(), "Zip");
            AddOperator(() => new ConcatOperatorController(), "Concat");
            AddOperator(() => new DocumentAppendOperatorController(), "Append");
            AddOperator(() => new ImageOperatorController(), "UriToImage");
            AddOperator(() => new FilterOperatorController(), "Filter", rfmc => new FilterOperatorBox(rfmc));
            AddOperator(() => new ApiOperatorController(), "Api", rfmc => new ApiOperatorBox(rfmc));
            AddOperator(() => new CompoundOperatorController(), "Compound");
            AddOperator(() => new MeltOperatorController(), "Melt", rfmc => new MeltOperatorBox(rfmc));
            AddOperator(() => new ExtractSentencesOperatorController(), "Sentence Analyzer", rfmc => new ExtractSentencesOperatorBox(rfmc));
            AddOperator(() => new ExtractKeywordsOperatorController(), "Extract KeyWords");
            AddOperator(() => new ImageRecognitionOperatorFieldModelController(), "ImageRecognition");

            //TODO fix DB special case
            //AddOperator<DBFilterOperatorController>("DBFilter", OperatorDocumentFactory.CreateDBFilterDocumentController, "⊇");

        }

        public static void AddOperator(Func<OperatorController> op, string title, Func<ReferenceController, CourtesyDocument> layoutFunc = null)
        {
            Operators[title] = new OperatorBuilder(() => OperatorDocumentFactory.CreateOperatorDocument(op(), title, layoutFunc), op, title);
        }


        // TODO fix DB special case
        public static void AddOperator<T>(string name, Func<DocumentController> docGeneratorFunc, string icon) where T : OperatorController, new()
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, () => new T(), name);
        }

        public static OperatorController GetOperatorController(string operatorType)
        {
            OperatorBuilder builder = null;
            Operators.TryGetValue(operatorType, out builder);
            return builder?.OperationControllerConstructor();
        }
    }
}
