using System.Linq;
using Newtonsoft.Json.Linq;

namespace CodeCrib.Wink.Lib
{
    public class Device
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public bool Connected { get; set; }
        public bool? Powered { get; set; }

        public static Device FromJson(string json)
        {
            return Device.FromJson(JToken.Parse(json));
        }

        public static Device FromJson(JToken deviceToken)
        {
            Device device = new Device();

            var deviceId = (deviceToken as JObject).Properties().Where(x => x.Name.EndsWith("_id")).Select(x => x.Name).FirstOrDefault();
            if (!string.IsNullOrEmpty(deviceId))
            {
                device.Id = (string)deviceToken.SelectToken(deviceId);
            }

            device.Type = (string)deviceToken.SelectToken("object_type");
            device.Name = (string)deviceToken.SelectToken("name");
            device.Model = (string)deviceToken.SelectToken("model_name");

            JToken lastReading = deviceToken.SelectToken("last_reading");
            device.Connected = (bool)lastReading.SelectToken("connection");

            //JToken desiredState = deviceToken.SelectToken("desired_state");

            JToken poweredToken = lastReading.SelectToken("powered");
            if (device.Connected == true && poweredToken != null)
            {
                device.Powered = poweredToken.Value<bool>();
            }
            else
            {
                device.Powered = false;
            }

            return device;
        }
    }
}