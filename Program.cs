using BuildSoft.VRChat.Osc;

using Libraries;

using static Libraries.WebAPIHelper;
using System.Net;
using BuildSoft.VRChat.Osc.Avatar;
using Newtonsoft.Json;

namespace Kanna_Protecc_OSC
{
    internal class Program
    {
        private static WebAPIHelper Helper;

        static async Task Main(string[] args)
        {
            OscUtility.Initialize();

            OscParameter.ValueChanged += OscParameterOnValueChanged;

            Helper = new WebAPIHelper(HandleData, "protecc", 3456);

            Console.WriteLine("Ready.");

            await Task.Delay(Timeout.Infinite);
        }

        public class KannaProteccKeysData
        {
            public string AvatarID = "Invalid";
            public Dictionary<string, object> Values = new Dictionary<string, object>();
        }

        public class Configuration
        {
            public List<KannaProteccKeysData> ProtecctedAvatarsData = new List<KannaProteccKeysData>();
        }

        private static ConfigLib<Configuration> Config = new ConfigLib<Configuration>(Environment.CurrentDirectory + "\\Config.json");

        private static async void HandleData(string Data, RequestType requestType, HttpListenerContext context)
        {
            if (requestType == RequestType.POST)
            {
                Helper.SendResponse(context, "OK", HttpStatusCode.OK);

                var KannaData = JsonConvert.DeserializeObject<KannaProteccKeysData>(Data);

                if (KannaData == null)
                {
                    return;
                }

                var index = Config.InternalConfig.ProtecctedAvatarsData.FindIndex(o => o.AvatarID == KannaData.AvatarID);

                if (index == -1)
                {
                    Config.InternalConfig.ProtecctedAvatarsData.Add(KannaData);
                }
                else
                {
                    Config.InternalConfig.ProtecctedAvatarsData[index] = KannaData;
                }

                Console.WriteLine($"Received And Saved OSC Settings For {KannaData.AvatarID}");
            }
        }

        private static void OscParameterOnValueChanged(IReadOnlyOscParameterCollection sender, ParameterChangedEventArgs e)
        {
            if (e.ValueSource != ValueSource.VRChat) return;

            var paramname = e.Address.Replace("/avatar/parameters/", "");
            if (paramname == "TrackingType" && (int)e.NewValue == 2)
            {
                foreach (var AvatarData in Config.InternalConfig.ProtecctedAvatarsData)
                    foreach (var Parse in AvatarData.Values)
                        OscParameter.SendAvatarParameter(Parse.Key, Parse.Value);
            }
        }
    }
}