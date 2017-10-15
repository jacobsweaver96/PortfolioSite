using SandyModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortfolioSite.Utils
{
    public static class PortfolioApiMediator
    {
        /// <summary>
        /// Gets a user from the portfolio api
        /// </summary>
        /// <param name="userName">The username of the user that is to be retrieved</param>
        /// <returns>The user</returns>
        public static User GetUser(string userName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Changes a provided user's password
        /// </summary>
        /// <param name="userId">The id of the user for which the password is being changed</param>
        /// <param name="hashedPass">The new hashed and salted user password</param>
        /// <param name="salt">The new salt</param>
        /// <returns>Success indicator</returns>
        public static bool ChangeUserPassword(int userId, string hashedPass, string salt)
        {
            throw new NotImplementedException();
        }
    }
}