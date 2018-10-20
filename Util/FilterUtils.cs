using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Dash.Models;

namespace Dash.StaticClasses
{
    /// <summary>
    /// Provides a series of utility functions for filtering documents.
    /// </summary>
    public static class FilterUtils
    {
        /// <summary>
        /// Takes in a list of DocumentModels and a FilterModel and returns a filtered list of DocumentModels
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<DocumentController> Filter(List<DocumentController> collection, FilterModel filter)
        {
            switch (filter.Type)
            {
                case FilterModel.FilterType.containsKey:
                    return CheckContainsKey(collection, filter.KeyName);
                case FilterModel.FilterType.valueContains:
                    //                    return CheckValueContains(collection, filter.Key, filter.Values);
                    return CheckValueContains(collection, filter.KeyName, filter.Value);
                case FilterModel.FilterType.valueEquals:
                    return CheckValueEquals(collection, filter.KeyName, filter.Value);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a list of keys in the collection that contain the specified text (to create autosuggestions)
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ImmutableHashSet<string> GetKeySuggestions(List<DocumentController> collection, string text)
        {
            var collectionKeys = new HashSet<string>();
            if (collection != null)
            {
                foreach (var doc in collection)
                {
                    var keyNames = new HashSet<string>();
                    foreach (var key in GetKeys(doc))
                    {
                        var keyName = key.Name;
                        keyNames.Add(keyName);
                    }
                    collectionKeys.UnionWith(keyNames);
                }
            }
            
            return collectionKeys.Where(k => k.ToLower().Contains(text)).ToImmutableHashSet();
        }

        /// <summary>
        /// Takes in a list of DocumentModels and a key and returns a list of DocumentModels whose dictionary
        /// contains the specified key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static List<DocumentController> CheckContainsKey(List<DocumentController> collection, string keyName)
        {
            var containsKeyDocuments = new List<DocumentController>();
            // loop through all documents in the collection to find the ones with the specified field
            foreach (var document in collection)
            {
                foreach (var docKey in GetKeys(document))
                {
                    if (docKey.Name.Equals(keyName))
                    {
                        containsKeyDocuments.Add(document);
                    }
                }
            }
            return containsKeyDocuments;
        }

        public static IEnumerable<KeyController> GetKeys(DocumentController doc)
        {

            foreach (var item in doc.EnumFields())
                yield return item.Key;
        }

        public static IEnumerable<KeyController> GetKeys(List<DocumentController> docs)
        {
            foreach (var doc in docs)
            {
                foreach (var key in GetKeys(doc))
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// multiple values
        /// Takes in a list of DocumentModels, a key, and an array of values, and returns a list of DocumentsModels whose dictionary
        /// contains a specified value at the specified key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        //private static List<DocumentController> CheckValueContains(List<DocumentController> collection, string key, string[] values)
        //{
        //    // use hashset to prevent duplicates
        //    var valueContainsDocuments = new HashSet<DocumentController>();

        //    // obtain a list of documents with the specified field using the CheckContainsKey method and loop through those documents
        //    foreach (var document in CheckContainsKey(collection, key))
        //    {
        //        // loop through all search values
        //        foreach (var value in values)
        //        {
        //            // add any documents whose dictionary contains the specified value at the specified key to the hashset of documents
        //            if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(document.GetField(key).Key, value, CompareOptions.IgnoreCase) >= 0)
        //            {
        //                valueContainsDocuments.Add(document);
        //            }
        //        }
        //    }
        //    return valueContainsDocuments.ToList();
        //}

        /// <summary>
        /// Takes in a list of DocumentModels, a key, and a value, and returns a list of DocumentsModels whose dictionary
        /// contains a specified value at the specified key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private static List<DocumentController> CheckValueContains(List<DocumentController> collection, string keyName, string value)
        {
            // use hashset to prevent duplicates
            var valueContainsDocuments = new HashSet<DocumentController>();

            // obtain a list of documents with the specified field using the CheckContainsKey method and loop through those documents
            foreach (var document in CheckContainsKey(collection, keyName))
            {
                KeyController key = null;
                foreach (var docKey in GetKeys(document))
                {
                    if (docKey.Name.Equals(keyName))
                    {
                        key = docKey;
                        break;
                    }
                }

                if (key == null)
                {
                    continue;
                }
                string data = "";
                var field = document.GetField(key);
                if (field is TextController)
                {
                    var text = field as TextController;
                    data = text.Data;
                }
                else if (field is ImageController)
                {
                    var image = field as ImageController;
                    data = image.ImageFieldModel.Data.AbsoluteUri;
                }
                if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(data, value, CompareOptions.IgnoreCase) >= 0)
                {
                    valueContainsDocuments.Add(document);
                }

                // add any documents whose dictionary contains the specified value at the specified key to the hashset of documents

            }
            return valueContainsDocuments.ToList();
        }

        /// <summary>
        /// Takes in a list of DocumentModels, a key, and a value, and returns a list of DocumentsModels whose dictionary
        /// contains only the specified value at the specified key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static List<DocumentController> CheckValueEquals(List<DocumentController> collection, string keyName, string value)
        {
            var valueEqualsDocuments = new List<DocumentController>();

            // loop through documents that have the specified field
            foreach (var document in CheckContainsKey(collection, keyName))
            {
                KeyController key = null;
                foreach (var docKey in GetKeys(document))
                {
                    if (docKey.Name == keyName)
                    {
                        key = docKey;
                        break;
                    }
                }

                if (key == null)
                {
                    continue;
                }
                string data = "";
                var field = document.GetField(key);
                if (field is TextController)
                {
                    var text = field as TextController;
                    data = text.Data;
                }
                else if (field is ImageController)
                {
                    var image = field as ImageController;
                    data = image.ImageFieldModel.Data.AbsoluteUri;
                }
                if (data.ToLower().Equals(value.ToLower()))
                {
                    valueEqualsDocuments.Add(document);
                }
            }
            return valueEqualsDocuments;
        }
    }
}
