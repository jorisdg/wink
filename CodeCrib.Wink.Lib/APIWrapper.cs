using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace CodeCrib.Wink.Lib
{
    // https://msdn.microsoft.com/en-us/windows/uwp/app-settings/store-and-retrieve-app-data

    public class Exception : System.Exception
    {
        public string Error { get; protected set; }
        public string ErrorDescription { get; protected set; }

        public Exception()
            : this("default", "Wink API Exception")
        {
        }
        public Exception(string error, string errorDescription)
            : base(errorDescription)
        {
            Error = error;
            ErrorDescription = errorDescription;
        }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Unauthorized", "Unauthorized")
        {
        }
        public UnauthorizedException(string error, string errorDescription)
            : base(error, errorDescription)
        {
        }
    }

    public class BadRequestException : Exception
    {
        public BadRequestException()
            : base("Bad Request", "Bad Request")
        {
        }
        public BadRequestException(string error, string errorDescription)
            : base(error, errorDescription)
        {
        }
    }

    public class LoginNeededException : Exception
    {
        public LoginNeededException()
            : base("Login Needed", "Login Needed")
        {
        }
        public LoginNeededException(string error, string errorDescription)
            : base(error, errorDescription)
        {
        }
    }

    internal class APIWrapper : IDisposable
    {
        Uri baseAddress = new Uri("https://api.wink.com/");
        string clientId;
        string clientSecret;

        HttpClient httpClient = null;

        public void CheckOAuthHeader(string token)
        {
            if (httpClient.DefaultRequestHeaders.Contains("Authorization"))
                httpClient.DefaultRequestHeaders.Remove("Authorization");

            if (!string.IsNullOrEmpty(token))
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
        }

        public APIWrapper(string clientid, string clientsecret)
            : base()
        {
            this.clientId = clientid;
            this.clientSecret = clientsecret;
            httpClient = new HttpClient { BaseAddress = baseAddress };
        }

        bool disposed = false;

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                httpClient.Dispose();
            }

            disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~APIWrapper()
        {
            this.Dispose(false);
        }

        async protected Task<string> GetResponse(HttpResponseMessage response)
        {
            string responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                JToken json = string.IsNullOrEmpty(responseData.Trim()) ? null : JObject.Parse(responseData);
                JToken data = (json != null) ? json.SelectToken("data") : null;
                string error = (data != null) ? (string)data.SelectToken("error") : null;
                string description = (data != null) ? (string)data.SelectToken("error_description") : null;

                if (string.IsNullOrEmpty(error))
                {
                    error = response.StatusCode.ToString();
                    if (string.IsNullOrEmpty(description))
                    {
                        description = string.Format("Http Error : {0}", response.StatusCode);
                    }
                }
                else if (string.IsNullOrEmpty(description))
                {
                    description = string.Format("{0} : {1}", response.StatusCode, error);
                }

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.BadRequest:
                        throw new BadRequestException(error, description);

                    case System.Net.HttpStatusCode.Unauthorized:
                        throw new UnauthorizedException(error, description);

                    default:
                        throw new Exception(error, description);
                }
            }

            return responseData;
        }

        protected StringContent CreateJSONContent(string message)
        {
            return new StringContent(message, System.Text.Encoding.UTF8, "application/json");
        }

        async protected Task<string> Post(string uri, string message)
        {
            string responseData = null;

            using (var content = CreateJSONContent(message))
            {
                using (var response = await httpClient.PostAsync(uri, content))
                {
                    responseData = await GetResponse(response);
                }
            }

            return responseData;
        }
        async protected Task<string> Put(string uri, string message)
        {
            string responseData = null;

            using (var content = CreateJSONContent(message))
            {
                using (var response = await httpClient.PutAsync(uri, content))
                {
                    responseData = await GetResponse(response);
                }
            }

            return responseData;
        }
        async protected Task<string> Get(string uri)
        {
            string responseData = null;

            using (var response = await httpClient.GetAsync(uri))
            {
                responseData = await GetResponse(response);
            }

            return responseData;
        }

        async public Task<string> GetToken(string userName, string password)
        {
            this.CheckOAuthHeader(null);

            return await Post("oauth2/token", string.Format("{{  \"client_id\": \"{0}\",  \"client_secret\": \"{1}\",  \"username\": \"{2}\",  \"password\": \"{3}\",  \"grant_type\": \"password\"}}", clientId, clientSecret, userName, password));
        }

        async public Task<string> RefreshToken(string refreshToken)
        {
            this.CheckOAuthHeader(null);

            return await Post("oauth2/token", string.Format("{{  \"client_id\": \"{0}\",  \"client_secret\": \"{1}\",  \"grant_type\": \"refresh_token\",  \"refresh_token\": \"{2}\"}}", clientId, clientSecret, refreshToken));
        }

        async public Task<string> GetAllDevices(string token)
        {
            return await Get("users/me/wink_devices");
        }

        async public Task<string> GetAllGroups(string token)
        {
            this.CheckOAuthHeader(token);

            return await Get("users/me/groups");
        }

        async public Task<string> UpdateGroupPoweredState(string token, string groupId, bool powered)
        {
            this.CheckOAuthHeader(token);

            return await Post(string.Format("groups/{0}/activate", groupId), string.Format("{{ \"desired_state\": {{ \"powered\": {0} }} }}", powered ? "true" : "false"));
        }

        async public Task<string> UpdateDevicePoweredState(string token, string deviceType, string deviceId, bool powered)
        {
            this.CheckOAuthHeader(token);

            return await Put(string.Format("{0}s/{1}/desired_state", deviceType, deviceId), string.Format("{{ \"desired_state\": {{ \"powered\": {0} }} }}", powered ? "true" : "false"));
        }
    }
}
