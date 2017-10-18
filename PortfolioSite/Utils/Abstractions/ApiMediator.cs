using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace PortfolioSite.Utils.Abstractions
{
    public abstract class ApiMediator
    {
        protected class EndpointBuilder
        {
            private string _endPointUrl = "";
            public string GetEndpoint()
            {
                return _endPointUrl;
            }

            public void AddSegment(object segment)
            {
                if (!string.IsNullOrWhiteSpace(_endPointUrl))
                {
                    _endPointUrl += "/";
                }

                _endPointUrl += segment.ToString();
            }
        }

        protected ApiMediator(string hostUrl)
        {
            HostUrl = hostUrl;
            _client = new RestClient(hostUrl);
        }

        protected ApiMediator(string hostUrl, string apiKey)
        {
            HostUrl = hostUrl;
            ApiKey = apiKey;
            _client = new RestClient(hostUrl);
        }

        protected ApiMediator(string hostUrl, List<KeyValuePair<string,string>> defaultHeaders)
        {
            HostUrl = hostUrl;
            DefaultHeaderValues = defaultHeaders;
            _client = new RestClient(hostUrl);
        }

        protected ApiMediator(string hostUrl, string apiKey, List<KeyValuePair<string, string>> defaultHeaders)
        {
            HostUrl = hostUrl;
            apiKey = _apiKey;
            DefaultHeaderValues = defaultHeaders;
            _client = new RestClient(hostUrl);
        }

        private string _hostUrl;
        public string HostUrl
        {
            get { return _hostUrl; }
            protected set
            {
                _hostUrl = value;
            }
        }

        private string _apiKey;
        public string ApiKey
        {
            get { return _apiKey; }
            protected set
            {
                _apiKey = value;
            }
        }

        private RestClient _client;

        protected List<KeyValuePair<string, string>> DefaultHeaderValues { get; set; } = new List<KeyValuePair<string, string>>();

        protected abstract void SetRequestAuthenticator(IRestRequest request);

        protected async virtual Task<IRestResponse<T>> SendRequest<T>(string endPointUrl, Method method,
                                                        object body = null,
                                                        List<KeyValuePair<string,string>> Headers = null,
                                                        List<KeyValuePair<string, string>> QueryParams = null) where T : new()
        {
            var request = new RestRequest(endPointUrl, method);
            var tHeaders = DefaultHeaderValues;
            SetRequestAuthenticator(request);
            
            if (Headers != null)
            {
                tHeaders = Headers;
            }
            
            foreach (var v in tHeaders)
            {
                request.AddHeader(v.Key, v.Value);
            }

            if (QueryParams != null)
            {
                foreach (var v in QueryParams)
                {
                    request.AddQueryParameter(v.Key, v.Value);
                }
            }

            if (method != Method.GET && body != null)
            {
                request.AddJsonBody(body);
            }

            var response = await _client.ExecuteTaskAsync<T>(request);

            return response;
        }
    }
}