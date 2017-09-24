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
        /// <returns></returns>
        Task<DocumentController> ParseAsync(IStorageFile item, string uniquePath);

    }
}
