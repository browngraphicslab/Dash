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
            AddOperator<DivideOperatorFieldModelController>("divide", "÷");
            AddOperator<IntersectionOperatorModelController>("intersection", "∩");
            AddOperator<UnionOperatorFieldModelController>("union", "∪");
            AddOperator<ZipOperatorFieldController>("zip");
            AddOperator<ConcatOperator>("concat");
            AddOperator<ImageOperatorFieldModelController>("uriToImage", "◑");
            AddOperator<FilterOperatorFieldModelController>("filter", OperatorDocumentModel.CreateFilterDocumentController, "⊇");
            AddOperator<ApiOperator>("api1", OperatorDocumentModel.CreateApi1DocumentController, "⚡");
            AddOperator<ApiOperatorController>("api", OperatorDocumentModel.CreateApiDocumentController, "⚡");
            AddOperator<CollectionMapOperator>("map", OperatorDocumentModel.CreateMapDocumentController, "⇨");
            AddOperator<CompoundOperatorFieldController>("compound", OperatorDocumentModel.CreateCompoundController, "💰");
        }

        public static void AddOperator<T>(string name) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new T()), () => new T(), name, name);
        }

        public static void AddOperator(string name, Func<DocumentController> docGeneratorFunc, Func<OperatorFieldModelController> opGeneratorFunc)
        {
            Operators[name] = new OperatorBuilder(docGeneratorFunc, opGeneratorFunc, name, name);
        }

        public static void AddOperator<T>(string name, string icon) where T : OperatorFieldModelController, new()
        {
            Operators[name] = new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new T()), () => new T(), name, icon);
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
