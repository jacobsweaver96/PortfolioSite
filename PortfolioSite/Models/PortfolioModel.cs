using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortfolioSite.Models
{
    public class PortfolioModel
    {
        public string UserName { get; set; }

        public string Biography { get; set; }

        public string ProfilePictureUri { get; set; }

        public string GithubUrl { get; set; }

        public List<string> ProfileLinkUrls { get; set; }

        public string BackgroundUri { get; set; }

        public bool IsUserLoggedIn { get; set; }
    }
}