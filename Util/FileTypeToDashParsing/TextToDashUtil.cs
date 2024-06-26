﻿using System;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Dash
{
    public class TextToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(FileData fileData, DataPackageView dataView = null)
        {
            string text;

            // if the uri filepath is a local file then copy it locally
            if (!fileData.File.FileType.EndsWith(".url"))
            {
                text = await FileIO.ReadTextAsync(fileData.File);
            }
            // otherwise stream it from the internet
            else
            {
                // Get access to a HTTP ressource
                using (var stream = await fileData.FileUri.GetHttpStreamAsync())
                {
                    // Read the contents as ASCII text
                    text = await stream.ReadTextAsync();
                }
            }
            var title = (fileData.File as StorageFile)?.DisplayName ?? fileData.File.Name;

            var doc = new PostitNote(text, title).Document;
            return doc;
        }
    }
}