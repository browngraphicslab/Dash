﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using DashShared;
using Microsoft.Toolkit.Uwp;

namespace Dash
{
    public class TextToDashUtil : IFileParser
    {

        public async Task<DocumentController> ParseFileAsync(FileData fileData)
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
            var doc = new NoteDocuments.PostitNote(text).Document;
            return doc;
        }
    }
}