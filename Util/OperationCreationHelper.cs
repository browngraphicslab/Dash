using System;
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
            var operatorTypes = typeof(OperatorController).Assembly.GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(OperatorController)));
            foreach (var operatorType in operatorTypes)
            {
                var title = ((KeyController) operatorType
                    .GetField("TypeKey", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null))?.KeyModel?.Name;

                Debug.Assert(title != null, $"The operator type {operatorType} does not have a static private field TypeKey");
                AddOperator(() => (OperatorController)Activator.CreateInstance(operatorType), title);
            }
        }

        public static void AddOperator(Func<OperatorController> op, string title)
        {
            Operators[title] = new OperatorBuilder(() => OperatorDocumentFactory.CreateOperatorDocument(op(), title), op, title);
        }
    }
}
