using System.Threading.Tasks;

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
