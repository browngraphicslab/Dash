using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public enum LinkDirection
    {
        ToSource,
        ToDestination
    }

    public enum LinkHandledResult
    {
        Unhandled,
        HandledClose,
        HandledRemainOpen
    }

    public interface ILinkHandler
    {
        /// <summary>
        /// Attempt to follow the given link
        /// </summary>
        /// <param name="linkDoc">The link doc to try to follow</param>
        /// <param name="direction">The direction to follow the link in</param>
        /// <returns></returns>

        //TODO: CONVERT BOOL TO ENUM

        LinkHandledResult HandleLink(DocumentController linkDoc, LinkDirection direction);
    }
}
