using PortfolioSite.Utils.Abstractions;
using RestSharp;
using SandyModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PortfolioSite.Utils
{
    public class PortfolioApiMediator : ApiMediator
    {
        public PortfolioApiMediator(string hostUrl, string apiKey) : base(hostUrl, apiKey) { }

        public PortfolioApiMediator(string hostUrl, string apiKey, List<KeyValuePair<string, string>> defaultHeaders) : base(hostUrl, apiKey, defaultHeaders) { }

        /// <summary>
        /// Gets a user from the portfolio api
        /// </summary>
        /// <param name="userName">The username of the user that is to be retrieved</param>
        /// <returns>The user</returns>
        public async Task<IRestResponse<User>> GetUserResponse(string userName)
        {
            var endPointBuilder = new EndpointBuilder();

            endPointBuilder.AddSegment("Users");
            endPointBuilder.AddSegment(userName);

            var endPoint = endPointBuilder.GetEndpoint();

            var queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("includeEndpoints", "false")
            };

            var response = await SendRequest<User>(endPoint, Method.GET, QueryParams: queryParams);

            return response;
        }

        /// <summary>
        /// Changes a provided user's password
        /// </summary>
        /// <param name="userId">The id of the user for which the password is being changed</param>
        /// <param name="hashedPass">The new hashed and salted user password</param>
        /// <param name="salt">The new salt</param>
        /// <returns>Success indicator</returns>
        public bool ChangeUserPassword(string userName, string hashedPass, string salt)
        {
            throw new NotImplementedException();
        }

        protected override void SetRequestAuthenticator(IRestRequest request)
        {
            request.AddHeader("Authentication", $"Basic {ApiKey}");
        }
    }
}