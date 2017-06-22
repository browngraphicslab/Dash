using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public class ImageOperatorModel : OperatorFieldModel
    {
        //Input keys
        public static readonly Key URIKey = new Key("A6D348D8-896B-4726-A2F9-EF1E8F1690C9", "URI");

        //Output keys
        public static readonly Key ImageKey = new Key("5FD13EB5-E5B1-4904-A611-599E7D2589AF", "Image");

        public override List<Key> Inputs { get; } = new List<Key> {URIKey};

        public override List<Key> Outputs { get; } = new List<Key> {ImageKey};

        public override Dictionary<Key, FieldModel> Execute(Dictionary<Key, ReferenceFieldModel> inputReferences)
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            Dictionary<Key, FieldModel> result = new Dictionary<Key, FieldModel>(1);

            TextFieldModel uri = docController.GetFieldInDocument(inputReferences[URIKey]) as TextFieldModel;
            Debug.Assert(uri != null, "Input is not a string");

            result[ImageKey] = new ImageFieldModel(new Uri(uri.Data));
            return result;
        }
    }
}
