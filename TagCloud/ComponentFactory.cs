using System;
using System.Collections.Generic;
using System.IO;
using Gma.CodeCloud.Controls.TextAnalyses;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist.En;
using Gma.CodeCloud.Controls.TextAnalyses.Extractors;
using Gma.CodeCloud.Controls.TextAnalyses.Stemmers;
using Gma.CodeCloud.Controls.TextAnalyses.Stemmers.En;

namespace TagCloud
{
    public static class ComponentFactory
    {

        public static IWordStemmer CreateWordStemmer(bool groupSameStemWords)
        {
            return groupSameStemWords
                       ? (IWordStemmer) new PorterStemmer()
                       : new NullStemmer();
        }

        public static IBlacklist CreateBlacklist(bool excludeEnglishCommonWords)
        {
            return excludeEnglishCommonWords
                       ? (IBlacklist) new CommonWords()
                       : new NullBlacklist();
        }

        public static IEnumerable<string> CreateExtractor(InputType inputType, string input, IProgressIndicator progress)
        {
            switch (inputType)
            {
                case InputType.File:
                    FileInfo fileInfo = new FileInfo(input);
                    return new FileExtractor(fileInfo, progress);

                case InputType.Uri:
                    Uri uri = new Uri(input);
                    return new UriExtractor(uri, progress);

                default:
                    return new StringExtractor(input, progress);
            }
        }

        //public static IProgressIndicator CreateProgressBar(InputType inputType, ProgressBar progressBar)
        //{
        //    return 
        //        inputType==InputType.String ? 
        //                                        new ProgressBarWrapper(progressBar) : 
        //                                                                                new InfiniteProgressBarWrapper(progressBar);
        //}

        public static InputType DetectInputType(string input)
        {
            if (input.Length < 0x200)
            {
                if (input.StartsWith("http"))
                {
                    return InputType.Uri;
                }
                try
                {
                    if (File.Exists(input))
                    {
                        return InputType.File;
                    }
                } catch (Exception)
                {

                }
            }
            return InputType.String;
        }
    }
}