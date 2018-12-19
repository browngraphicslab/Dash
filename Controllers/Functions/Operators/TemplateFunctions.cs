using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class TemplateFunctions
    {
        public static TextController ContentTemplate(TextController keyName)
        {
            return InsertContent(keyName, (int)TemplateList.TemplateType.Content);
        }

        public static TextController TextTemplate(TextController keyName)
        {
            return InsertContent(keyName, (int)TemplateList.TemplateType.Title);
        }

        public static TextController ImageTemplate(TextController keyName)
        {
            return InsertContent(keyName, (int)TemplateList.TemplateType.Image);
        }

        [GeneratorIgnore]
        private static TextController InsertContent(TextController keyName, int templateType)
        {
            string preXaml = TemplateList.Templates[templateType].GetXaml();
            string[] splitXaml = preXaml.Split(" ", StringSplitOptions.None);
            for (int j = 0; j < splitXaml.Length; j++)
            {
                if (splitXaml[j].Contains("Field0"))
                {
                    splitXaml[j] = splitXaml[j].Replace("0", keyName.Data);
                    break;
                }
            }
            string postXaml = string.Join(" ", splitXaml);
            return new TextController(postXaml);

        }
    }
}
