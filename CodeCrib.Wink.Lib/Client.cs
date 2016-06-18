using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCrib.Wink.Lib
{
    public class Auth
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }

        public Auth(string token, string refreshToken)
        {
            Token = token;
            RefreshToken = refreshToken;
        }

        public static Auth FromJson(string json)
        {
            return Auth.FromJson(JToken.Parse(json));
        }

        public static Auth FromJson(JToken deviceToken)
        {
            string token, refreshToken;

            token = (string)deviceToken.SelectToken("access_token");
            refreshToken = (string)deviceToken.SelectToken("refresh_token");

            return new Auth(token, refreshToken);
        }
    }

    public class Client
    {
        APIWrapper api;
        public Auth oAuth { get; protected set; }

        public Client(string clientId, string clientSecret, string oAuthToken, string oAuthRefreshToken)
            : this(clientId, clientSecret, new Auth(oAuthToken, oAuthRefreshToken))
        {
        }

        public Client(string clientId, string clientSecret, Auth oAuth)
        {
            api = new APIWrapper(clientId, clientSecret);
            this.oAuth = oAuth;
        }

        public async Task<Auth> Authenticate()
        {
            if (oAuth != null && !string.IsNullOrEmpty(oAuth.RefreshToken))
            {
                try
                {
                    var json = await api.RefreshToken(oAuth.RefreshToken);

                    JToken jsonDevices = JObject.Parse(json);
                    var data = jsonDevices.SelectToken("data");

                    oAuth = Auth.FromJson(data);
                }
                catch(BadRequestException)
                {
                    oAuth = null;
                    throw new LoginNeededException("Refresh failed", "Please log in again.");
                }
            }
            else
            {
                oAuth = null;
            }

            return oAuth;
        }
        public async Task<Auth> Authenticate(string userName, string password)
        {
            oAuth = null;

            var json = await api.GetToken(userName, password);

            JToken jsonDevices = JObject.Parse(json);
            var data = jsonDevices.SelectToken("data");

            oAuth = Auth.FromJson(data);

            return oAuth;
        }

        public async Task<Device[]> GetAllDevices()
        {
            Device[] devices = null;

            try
            {
                var json = await api.GetAllDevices(this.oAuth.Token);

                JToken jsonDevices = JObject.Parse(json);
                var data = jsonDevices.SelectToken("data");

                devices = new Device[data.Count()];

                for (int i = 0; i < data.Count(); i++)
                {
                    JToken device = data[i];
                    devices[i] = Device.FromJson(device);
                }
            }
            catch(UnauthorizedException)
            {
                if (await this.Authenticate() != null)
                {
                    await GetAllDevices();
                }
            }

            return devices;
        }

        public async Task<Group[]> GetAllGroups()
        {
            Group[] groups = null;

            try
            {
                var json = await api.GetAllGroups(this.oAuth.Token);

                JToken jsonGroups = JObject.Parse(json);
                var data = jsonGroups.SelectToken("data");

                groups = new Group[data.Count()];

                for (int i = 0; i < data.Count(); i++)
                {
                    JToken group = data[i];
                    groups[i] = Group.FromJson(group);
                }
            }
            catch(UnauthorizedException)
            {
                if (await this.Authenticate() != null)
                {
                    await GetAllGroups();
                }
            }

            return groups;
        }

        public async Task SetGroup(Group group, bool powered)
        {
            try
            {
                var json = await api.UpdateGroupPoweredState(this.oAuth.Token, group.Id, powered);
            }
            catch (UnauthorizedException)
            {
                if (await this.Authenticate() != null)
                {
                    await SetGroup(group, powered);
                }
            }
        }

        public async Task SetDevice(Device device, bool powered)
        {
            try
            {
                var json = await api.UpdateDevicePoweredState(this.oAuth.Token, device.Type, device.Id, powered);
            }
            catch (UnauthorizedException)
            {
                if (await this.Authenticate() != null)
                {
                    await SetDevice(device, powered);
                }
            }
        }
    }
}
