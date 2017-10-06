using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dash
{
    public interface IFileParser
    {
        /// <summary>
        /// Parses the passed in storage item returning a document controller
        /// the unique path can be used by the parser to identify the file
        /// </summary>
        /// <param name="item"></param>
        /// <param name="uniquePath"></param>
        /// <returns>The document controller representing the DATA portion of the file that was parsed. Thus if any
        /// special layout logic should be applied to it, that layout logic should be applied as an ActiveLayout</returns>
        Task<DocumentController> ParseFileAsync(IStorageFile item, string uniquePath=null);

    }
}
