using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Dash
{
    public interface IFileParser
    {
        /// <summary>
        /// Parses the passed in file data returning a document controller
        /// </summary>
        Task<DocumentController> ParseFileAsync(FileData fileData);
    }
}
