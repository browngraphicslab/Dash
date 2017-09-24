using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dash
{
    public class CsvToDashUtil : IFileParser
    {
        public Task<DocumentController> ParseAsync(IStorageFile item, string uniquePath)
        {
            throw new NotImplementedException();
        }
    }
}
