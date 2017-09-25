using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dash
{
    public class PptToDashUtil : IFileParser
    {

        public Task<DocumentController> ParseFileAsync(IStorageFile item, string uniquePath)
        {
            throw new NotImplementedException();
        }
    }
}
