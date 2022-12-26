using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("VK Status", "klauz24", "1.0.0")]
    internal class VKStatus : CovalencePlugin
    {
        private PluginConfig _config;

        private class PluginConfig
        {
            [JsonProperty("VK Token")]
            public string Token;

            [JsonProperty("VK Group Id")]
            public string GroupId;

            [JsonProperty("Status")]
            public string Status;
        }

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig()
            {
                Token = "VK_TOKEN_HERE",
                GroupId = "VK_GROUP_ID_HERE",
                Status = "🔥 Хартфан | 📈 Онлайн: {current}/{max} | ♻ Вайп каждую пятницу!"
            };
        }

        private void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        private void OnServerInitialized()
        {
            if (_config.Token == "VK_TOKEN_HERE" || _config.GroupId == "VK_GROUP_ID_HERE")
            {
                PrintError($"Token or group id was not set.");
            }
            else
            {
                timer.Every(300f, () =>
                {
                    var current = server.Players.ToString();
                    var max = server.MaxPlayers.ToString();
                    var status = _config.Status.Replace("{current}", current).Replace("{max}", max);
                    webrequest.Enqueue("https://api.vk.com/method/status.set", $"group_id={_config.GroupId}&text={status}&access_token={_config.Token}&v=5.64", (code, response) =>
                    {
                        if (code != 200 || response == null)
                        {
                            PrintWarning("Failed to update VK group status!");
                            return;
                        }
                    }, this);
                });
            }
        }
    }
}