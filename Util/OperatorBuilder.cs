using System;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class OperatorBuilder
    {
        public OperatorBuilder(Func<DocumentController> operationConstructor, string name, string icon)
        {
            OperationConstructor = operationConstructor;
            Name = name;
            Icon = icon;
        }

        public Func<DocumentController> OperationConstructor { get; }
        public string Name { get; }
        public string Icon { get; }
    }
}
