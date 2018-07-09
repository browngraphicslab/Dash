namespace Dash
{
    public class StringSearchModel
    {
        public bool StringFound { get; private set; }
        public string RelatedString { get; private set; }
        public bool IsUseFullRelatedString { get; private set; }

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
        /// <param name="isUseFullRelatedString"></param>
        public StringSearchModel(string relatedString, bool isUseFullRelatedString = false)
        {
            RelatedString = relatedString;
            StringFound = true;
            IsUseFullRelatedString = isUseFullRelatedString;
        }

        private StringSearchModel() { }

        public static StringSearchModel False { get; } = new StringSearchModel(){StringFound = false};

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
