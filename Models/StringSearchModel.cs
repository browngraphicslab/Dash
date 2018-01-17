using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class StringSearchModel
    {
        public bool StringFound { get; private set; }
        public string RelatedString { get; private set; }

        /// <summary>
        /// probably shouldn't ever need to use this constructor
        /// </summary>
        /// <param name="stringFound"></param>
        /// <param name="relatedString"></param>
        public StringSearchModel(bool stringFound, string relatedString)
        {
            StringFound = stringFound;
            RelatedString = relatedString;
        }

        /// <summary>
        /// constructor to use when you have a true result
        /// </summary>
        /// <param name="relatedString"></param>
        public StringSearchModel(string relatedString)
        {
            RelatedString = relatedString;
            StringFound = true;
        }

        private StringSearchModel() { }

        public static StringSearchModel False
        {
            get { return new StringSearchModel() {StringFound = false}; }
        }

        public override string ToString()
        {
            if (!StringFound && RelatedString == null)
            {
                return "StringSearchModel.FALSE";
            }
            return base.ToString();
        }
    }
}
