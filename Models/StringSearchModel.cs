namespace Dash
{
    public class StringSearchModel
    {
        public bool StringFound { get; private set; }
        public string RelatedString { get; private set; }

        /// <summary>
        /// constructor to use when you have a true result
        /// </summary>
        /// <param name="relatedString"></param>
        /// <param name="isUseFullRelatedString"></param>
        public StringSearchModel(string relatedString)
        {
            RelatedString = relatedString;
            StringFound = true;
        }

        private StringSearchModel() { }

        public static StringSearchModel False { get; } = new StringSearchModel {StringFound = false};

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
