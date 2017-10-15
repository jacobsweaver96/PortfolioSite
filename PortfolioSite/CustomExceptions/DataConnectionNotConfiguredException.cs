using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortfolioSite.CustomExceptions
{
    public class DataConnectionNotConfiguredException : Exception
    {
        public DataConnectionNotConfiguredException(string message) : base(message) { }
    }
}