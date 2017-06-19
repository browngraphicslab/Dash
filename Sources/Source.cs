using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash.Sources {
    /// <summary>
    /// This class abstracts default functionality for a "Source", an
    /// object that is used to generate document data from either user
    /// input directly to the Dash program orfrom  an already existing 
    /// source.
    /// </summary>
    /// <remarks>
    /// Examples of sources from existing data include: 
    ///     / Web APIs / Files from Disk /
    /// Examples of sources from user-inputted data include:
    ///     / Text / Ink /
    /// </remarks>
   public abstract class Source {
        string generatedDocumentType; // docs made by this source have this type

        // == METHODS ==

        /// <summary>
        /// Generates a given document of type generatedDocumentType.
        /// </summary>
        /// <returns>Generated document</returns>
        public abstract DocumentModel generateDocument();
    }
}
