using System.Collections.Generic;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public static partial class OperationCreationHelper
    {

        public static List<OperatorBuilder> Operators { get; }

        static OperationCreationHelper()
        {
            Operators = new List<OperatorBuilder>
            {
                new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController()), "divide", "÷"),
                new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new IntersectionOperatorModelController()), "intersection", "∩"),
                new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new ImageOperatorFieldModelController()), "uriToImage", "◑"),
                new OperatorBuilder(() => OperatorDocumentModel.CreateOperatorDocumentModel(new UnionOperatorFieldModelController()), "union", "∪"),
                new OperatorBuilder(OperatorDocumentModel.CreateFilterDocumentController, "filter", "⊇"),
                new OperatorBuilder(OperatorDocumentModel.CreateApiDocumentController, "api", "⚡"),
                new OperatorBuilder(OperatorDocumentModel.CreateMapDocumentController, "map", "⇨"),
                new OperatorBuilder(OperatorDocumentModel.CreateCompoundController, "compound", "💰"),
            };
        }
    }
}
