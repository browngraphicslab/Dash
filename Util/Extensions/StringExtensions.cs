// ReSharper disable once CheckNamespace
namespace Dash
{
    public static class StringExtensions
    {
        public static string RemoveWhitespace(this string str) => str.Trim(' ', '\r', '\n', '\t');
    }
}