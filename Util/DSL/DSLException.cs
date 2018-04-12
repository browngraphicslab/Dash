using System;

namespace Dash
{
    public abstract class DSLException : Exception
    {
        public abstract string GetHelpfulString();
    }

}
