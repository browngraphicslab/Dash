using System.Linq;
using System.Text.RegularExpressions;

namespace DashShared.Models
{
    public class FilterModel
    {

        public enum FilterType { containsKey, valueContains, valueEquals };

        /// <summary>
        /// The mode in which the user filtered in
        /// </summary>
        public FilterType Type { get; }
        /// <summary>
        /// String representing the filter key (field specified)
        /// </summary>
        public string KeyName { get; }
        /// <summary>
        /// Array of filter values obtained by removing all punctuation and spaces from the original
        /// string and splitting it
        /// </summary>
        public string[] Values { get; }
        /// <summary>
        /// Original string representing the filter value
        /// </summary>
        public string Value { get; }

        public FilterModel(FilterType filterType, string keyName, string value)
        {
            Type = filterType;
            KeyName = keyName;
            Value = value;
            // remove all punctuations and spaces from string and split it into an array of search words
            Values = Regex.Replace(value, @"\p{P}", " ").Split(' ').Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
            // Values = new string(value.Where(c => !char.IsPunctuation(c)).ToArray()).Split(' ');
        }
    }
}
