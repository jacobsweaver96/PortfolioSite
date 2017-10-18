using log4net;
using PortfolioSite.CustomExceptions;
using PortfolioSite.Data;
using SandyUtils.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Data.Entity;

namespace PortfolioSite.Utils
{
    /// <summary>
    /// Model for login results
    /// </summary>
    public class LoginResult
    {
        public LoginResult() { }
        public LoginResult(bool isSuccess, string authToken = null)
        {
            IsLoginSuccess = isSuccess;
            AuthToken = authToken;
        }

        public bool IsLoginSuccess { get; private set; }

        public string AuthToken { get; private set; }
    }

    /// <summary>
    /// Util for managing sessions
    /// </summary>
    public class SessionManager
    {
        private ILog logger
        {
            get
            {
                return DependencyResolver.Resolve<ILog>();
            }
        }

        private PortfolioApiMediator pfApiMediator
        {
            get
            {
                return DependencyResolver.Resolve<PortfolioApiMediator>();
            }
        }

        #region Singleton
        private static SessionManager _current;

        private SessionManager() { }

        public static SessionManager Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new SessionManager();
                }

                return _current;
            }
        }
        #endregion

        #region Configurations
        private const int DEFAULT_SESSION_LENGTH_MIN = 60;

        private volatile string _ctxConnstring;
        public string ContextConnectionString
        {
            get { return _ctxConnstring; }
            set
            {
                _ctxConnstring = value;
            }
        }

        private TimeSpan _sessionLength = new TimeSpan(0, DEFAULT_SESSION_LENGTH_MIN, 0);
        public int SessionLengthInMinutes
        {
            get
            {
                if (_sessionLength == null)
                {
                    return 0;
                }

                return (int)Math.Min(_sessionLength.TotalMinutes, int.MaxValue);
            }
            set
            {
                _sessionLength = new TimeSpan(0, value, 0);
            }
        }
        #endregion

        private async Task ContextExec(Func<PortfolioTokenDBEntities, Task> exec)
        {
            if (string.IsNullOrWhiteSpace(_ctxConnstring))
            {
                throw new DataConnectionNotConfiguredException("The data connection has not been configured for the session manager");
            }

            using (PortfolioTokenDBEntities ctx = new PortfolioTokenDBEntities(_ctxConnstring))
            {
                await exec(ctx);
            }
        }

        private async Task<T> ContextExec<T>(Func<PortfolioTokenDBEntities, Task<T>> exec)
        {
            if (string.IsNullOrWhiteSpace(_ctxConnstring))
            {
                throw new DataConnectionNotConfiguredException("The data connection has not been configured for the session manager");
            }

            T ret = default(T);
            PortfolioTokenDBEntities ctx = null;

            try
            {
                using (ctx = new PortfolioTokenDBEntities(_ctxConnstring))
                {
                    ret = await exec(ctx);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failure on database contextual execution", ex);
            }

            return ret;
        }

        /// <summary>
        /// Tries to login
        /// </summary>
        /// <param name="userName">The user name of the user attempting to login</param>
        /// <param name="password">The plaintext password of the user attempting to login</param>
        /// <returns>The result of the login</returns>
        public async Task<LoginResult> TryLogin(string userName, string password)
        {
            LoginResult result = new LoginResult(false);

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return result;
            }

            var userResponse = await pfApiMediator.GetUserResponse(userName);

            if (userResponse == null || userResponse.Data == null)
            {
                return result;
            }

            var user = userResponse.Data;
            var doesPasswordMatch = SecurityUtil.VerifyPassword(password, user.Salt, user.Password);

            result = await ContextExec(async (ctx) =>
            {
                var token = Guid.NewGuid().ToString("N");

                ctx.UserSessions.Add(new UserSession
                {
                    UserId = user.UserId,
                    Expiration = GetSessionExpirationTime(),
                    Token = token,
                    IsExpired = false,
                });

                await ctx.SaveChangesAsync();

                return new LoginResult(true, token);
            });

            return result;
        }

        /// <summary>
        /// Logs out a user
        /// </summary>
        /// <param name="authToken">The auth token associated with the session that is being terminated</param>
        public async Task<bool> Logout(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken) || authToken.Length != 32)
            {
                return false;
            }
            
            var ret = await ContextExec(async (ctx) =>
            {
                var session = await ctx.UserSessions.SingleOrDefaultAsync(v => v.Token == authToken);
                session.IsExpired = true;
                await ctx.SaveChangesAsync();
                return true;
            });

            return ret;
        }

        /// <summary>
        /// Checks if a provided token is valid and not expired
        /// </summary>
        /// <param name="authToken">The auth token</param>
        /// <returns>Indicator of whether or not the token is valid</returns>
        public async Task<bool> IsTokenValid(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
            {
                return false;
            }

            var result = await ContextExec((ctx) =>
            {
                return new Task<bool>(() =>
                {
                    var session = ctx.UserSessions.SingleOrDefault(v => v.Token == authToken);

                    return session != null && !IsSessionExpired(session);
                });
            });

            return result;
        }

        /// <summary>
        /// Tries to refresh a given token's expiration time
        /// </summary>
        /// <param name="authToken">The auth token</param>
        /// <returns>Success indicator</returns>
        public async Task RefreshToken(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
            {
                return;
            }

            await ContextExec(async (ctx) =>
            {
                var session = await ctx.UserSessions.SingleOrDefaultAsync(v => !IsSessionExpired(v) && v.Token == authToken);

                if (session != null)
                {
                    session.Expiration = GetSessionExpirationTime();
                    await ctx.SaveChangesAsync();
                }
            });

            return;
        }

        #region Helper
        private bool IsSessionExpired(UserSession session)
        {
            return session.IsExpired || session.Expiration <= DateTime.Now;
        }

        private DateTime GetSessionExpirationTime()
        {
            return DateTime.Now + _sessionLength;
        }
        #endregion
    }
}