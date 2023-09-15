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
            OscAvatarUtility.AvatarChanged += OscAvatarUtilityOnAvatarChanged;

            Helper = new WebAPIHelper(HandleData, "protecc", 3456);

            Console.WriteLine("Ready.");

            await Task.Delay(Timeout.Infinite);
        }

        private static void OscAvatarUtilityOnAvatarChanged(OscAvatar sender, ValueChangedEventArgs<OscAvatar> e)
        {
            Console.WriteLine($"Changed Avatar To {e.NewValue.Id}");
            Console.WriteLine($"Has Config: {e.NewValue.ToConfig() != null}");
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
                // Send a 200 OK response
                Helper.SendResponse(context, "OK", HttpStatusCode.OK);

                // Tip: Data will be in json format if the request was formurlencoded, otherwise it will be the raw data.
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
            if (e.ValueSource == ValueSource.VRChat)
            {
                var paramname = e.Address.Replace("/avatar/parameters/", "");

                if (paramname == "TrackingType")
                {
                    if ((int)e.NewValue == 2)
                    {
                        Console.WriteLine($"Possible Avatar Reset Detected");
                    }
                }

                Console.WriteLine($"Param Update: {paramname} From {e.OldValue} To {e.NewValue}");

                var match = Config.InternalConfig.ProtecctedAvatarsData.FirstOrDefault(o => o.Values.ContainsKey(paramname));

                if (match != null)
                {
                    if ((bool)match.Values[paramname] != (bool)e.NewValue)
                    {
                        Console.WriteLine($"Fixing Param: {paramname} From {e.NewValue} To {match.Values[paramname]}");
                        OscParameter.SendAvatarParameter(paramname, match.Values[paramname]);
                    }
                }
            }
        }
    }
}