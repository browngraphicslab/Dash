using System;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class OperatorBuilder
    {

        /// <summary>
        ///     A func which returns a document containing an operator
        /// </summary>
        public Func<DocumentController> OperationDocumentConstructor { get; }

        /// <summary>
        ///     A func which returns an OperatorFieldModelController
        /// </summary>
        public Func<OperatorFieldModelController> OperationControllerConstructor { get; }

        /// <summary>
        ///     The title of the operator, can be null
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///     The operator builder class is a helper class which provides functions for building documents containing operators
        ///     and operator controllers.
        /// </summary>
        /// <param name="operationDocumentConstructor">A func which returns a document containing an operator</param>
        /// <param name="operationControllerConstructor">A func which returns an OperatorFieldModelController</param>
        /// <param name="title">The title of the operator, can be null</param>
        public OperatorBuilder(Func<DocumentController> operationDocumentConstructor,
            Func<OperatorFieldModelController> operationControllerConstructor, string title)
        {
            OperationDocumentConstructor = operationDocumentConstructor;
            OperationControllerConstructor = operationControllerConstructor;
            Title = title;
        }
    }
}