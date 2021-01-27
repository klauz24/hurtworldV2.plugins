using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;

namespace Oxide.Plugins
{
    [Info("Player Count Discord Bot", "klauz24", "1.0.0")]
    internal class PlayerCountDiscordBot : HurtworldPlugin
    {
        [DiscordClient] private DiscordClient _client;

        protected override void LoadDefaultConfig()
        {
            Config["UpdateInterval"] = 10f;
            Config["Token"] = "";
            Config["Status"] = "{0}/{1}";
        }

        private void OnServerInitialized()
        {
            var token = Config["Token"].ToString();
            if (token != "")
            {
                Discord.CreateClient(this, token);
                timer.In(3f, () => UpdateStatus());
                timer.Every(float.Parse(Config["UpdateInterval"].ToString()), () => UpdateStatus());
                timer.Every(300f, () => Server.Command("o.reload PlayerCountDiscordBot"));
            }
            else
            {
                Puts("Discord token is not setup.");
            }
        }

        private void UpdateStatus()
        {
            _client.UpdateStatus(new Presence()
            {
                Game = new Ext.Discord.DiscordObjects.Game()
                {
                    Name = string.Format(Config["Status"].ToString(), GameManager.Instance.GetPlayerCount(), GameManager.Instance.ServerConfig.MaxPlayers),
                    Type = ActivityType.Game
                }
            });
        }
    }
}
