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

        private void ContextExec(Action<SiteAuthDataContext> exec)
        {
            if (string.IsNullOrWhiteSpace(_ctxConnstring))
            {
                throw new DataConnectionNotConfiguredException("The data connection has not been configured for the session manager");
            }

            var bt = new Thread(new ParameterizedThreadStart((cancelToken) =>
            {
                SiteAuthDataContext ctx = null;

                if (!(cancelToken is CancelToken))
                {
                    logger.Warn("Invalid cancellation token provided");
                }

                try
                {
                    using (ctx = new SiteAuthDataContext(_ctxConnstring))
                    {
                        exec(ctx);
                    }
                }
                catch (ThreadAbortException ex)
                {
                    if (!ThreadManager.Current.IsSafeAbort)
                    {
                        logger.Warn("Unsafe abortion of database access", ex);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Exception on database access", ex);
                }
                finally
                {
                    if (ctx != null)
                    {
                        ctx.Dispose();
                    }
                }
            }));

            ThreadManager.Current.AddThread(new ThreadWrapper(bt, new CancelToken()));

            bt.Start(_ctxConnstring);
        }

        private async Task<T> ContextExec<T>(Func<SiteAuthDataContext,Task<T>> exec)
        {
            if (string.IsNullOrWhiteSpace(_ctxConnstring))
            {
                throw new DataConnectionNotConfiguredException("The data connection has not been configured for the session manager");
            }

            T ret = default(T);
            SiteAuthDataContext ctx = null;

            try
            {
                using (ctx = new SiteAuthDataContext(_ctxConnstring))
                {
                    ret = await exec(ctx);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failure on database contextual execution", ex);
            }
            finally
            {
                if (ctx != null)
                {
                    ctx.Dispose();
                }
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
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return new LoginResult(false);
            }

            var user = PortfolioApiMediator.GetUser(userName);

            if (user == null)
            {
                return new LoginResult(false);
            }

            var doesPasswordMatch = SecurityUtil.VerifyPassword(password, user.Salt, user.Password);
            LoginResult result = new LoginResult(false);


            result = await ContextExec(async (ctx) =>
            {
                var token = Guid.NewGuid().ToString("N");

                ctx.UserSessions.InsertOnSubmit(new UserSession
                {
                    UserId = user.UserId,
                    Expiration = GetSessionExpirationTime(),
                    Token = token,
                    IsExpired = false,
                });
                ctx.SubmitChanges();

                return new LoginResult(true, token);
            });

            return result;
        }

        /// <summary>
        /// Logs out a user
        /// </summary>
        /// <param name="authToken">The auth token associated with the session that is being terminated</param>
        public void Logout(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken) || authToken.Length != 32)
            {
                return;
            }
            
            ContextExec((ctx) =>
            {
                var session = ctx.UserSessions.SingleOrDefault(v => v.Token == authToken);
                session.IsExpired = true;
                ctx.SubmitChanges();
            });
        }

        /// <summary>
        /// Checks if a provided token is valid and not expired
        /// </summary>
        /// <param name="authToken">The auth token</param>
        /// <returns>Indicator of whether or not the token is valid</returns>
        public bool IsTokenValid(string authToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to refresh a given token's expiration time
        /// </summary>
        /// <param name="authToken">The auth token</param>
        /// <returns>Success indicator</returns>
        public bool TryRefreshToken(string authToken)
        {
            throw new NotImplementedException();
        }

        #region Helper
        private bool IsSessionExpired(DateTime expTime)
        {
            return expTime <= DateTime.Now;
        }

        private DateTime GetSessionExpirationTime()
        {
            return DateTime.Now + _sessionLength;
        }
    }
}