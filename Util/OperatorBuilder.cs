using System;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class OperatorBuilder
    {
        public OperatorBuilder(Func<DocumentController> operationDocumentConstructor, Func<OperatorFieldModelController> operationControllerConstructor, string name, string icon)
        {
            OperationDocumentConstructor = operationDocumentConstructor;
            OperationControllerConstructor = operationControllerConstructor;
            Name = name;
            Icon = icon;
        }

        public Func<DocumentController> OperationDocumentConstructor { get; }
        public Func<OperatorFieldModelController> OperationControllerConstructor { get; }
        public string Name { get; }
        public string Icon { get; }
    }
}
