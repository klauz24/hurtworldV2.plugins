using System.Linq;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;

namespace Oxide.Plugins
{
    [Info("Player Count for Discord Ext. v.1.x.x", "klauz24", "1.0.0")]
    internal class PlayerCount : CovalencePlugin
    {
        [DiscordClient] DiscordClient Client;

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["Token"] = "DISCORD_TOKEN";
            Config["Format"] = "{current}/{max}";
            Config["Refresh rate"] = 30;
        }

        private void OnServerInitialized()
        {
            var token = Config["Token"].ToString();
            if (token == "DISCORD_TOKEN")
            {
                PrintError("You did not setup your Discord token in the config file!");
                return;
            }
            Discord.CreateClient(this, token);
            var refreshRate = Config["Refresh rate"].ToString();
            timer.Every(int.Parse(refreshRate), () =>
            {
                Client.UpdateStatus(new Presence()
                {
                    Game = new Ext.Discord.DiscordObjects.Game
                    {
                        Name = Config["Format"].ToString().Replace("{current}", players.Connected.Count().ToString()).Replace("{max}", server.MaxPlayers.ToString()),
                        Type = ActivityType.Game
                    },
                    Status = "online",
                    Since = 0,
                    AFK = false
                });
            });
            timer.Every(300, () => server.Command("o.reload PlayerCount"));
        }
    }
}