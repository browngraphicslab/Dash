using Dash.Controllers.Operators;
using System;
using System.Collections.Generic;
using System.Linq;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static partial class OperationCreationHelper
    {

        public static ObservableDictionary<string, OperatorBuilder> Operators { get; } = new ObservableDictionary<string, OperatorBuilder>();

        static OperationCreationHelper()
        {
            AddOperator<DivideOperatorFieldModelController>("Divide", "÷");
            AddOperator<IntersectionOperatorModelController>("Intersection", "∩");
            AddOperator<UnionOperatorFieldModelController>("Union", "∪");
            AddOperator<ZipOperatorFieldController>("Zip");
            AddOperator<DBFilterOperatorFieldModelController>("DBFilter", OperatorDocumentModel.CreateDBFilterDocumentController, "⊇");
            AddOperator<ConcatOperator>("Concat");
            AddOperator<DocumentAppendOperatorController>("Append");
            AddOperator<ImageOperatorFieldModelController>("UriToImage", "◑");
            AddOperator<FilterOperatorFieldModelController>("Filter", OperatorDocumentModel.CreateFilterDocumentController, "⊇");
            AddOperator<ApiOperatorController>("Api", OperatorDocumentModel.CreateApiDocumentController, "⚡");
            AddOperator<CollectionMapOperator>("Map", OperatorDocumentModel.CreateMapDocumentController, "⇨");
            AddOperator<CompoundOperatorFieldController>("Compound", OperatorDocumentModel.CreateCompoundController, "💰");
        }

        public static void AddOperator<T>(string name) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new T(), name), () => new T(), name, name);
        }

        public static void AddOperator(string name, Func<DocumentController> docGeneratorFunc, Func<OperatorFieldModelController> opGeneratorFunc)
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, opGeneratorFunc, name, name);
        }

        public static void AddOperator<T>(string name, string icon) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new T(), name), () => new T(), name, icon);
        }

        public static void AddOperator(string name, Func<DocumentController> docGeneratorFunc, Func<OperatorFieldModelController> opGeneratorFunc, string icon)
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, opGeneratorFunc, name, icon);
        }

        public static void AddOperator<T>(string name, Func<DocumentController> docGeneratorFunc, string icon) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, () => new T(), name, icon);
        }

        public static DocumentController GetOperatorDocument(string operatorType)
        {
            OperatorBuilder builder = null;
            Operators.TryGetValue(operatorType, out builder);
            return builder?.OperationDocumentConstructor();
        }

        public static OperatorFieldModelController GetOperatorController(string operatorType)
        {
            OperatorBuilder builder = null;
            Operators.TryGetValue(operatorType, out builder);
            return builder?.OperationControllerConstructor();
        }
    }
}
