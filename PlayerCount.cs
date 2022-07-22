using System;
using System.Linq;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;

namespace Oxide.Plugins
{
    [Info("Player Count for Discord Ext. v.1.x.x", "klauz24", "1.0.1")]
    internal class PlayerCount : CovalencePlugin
    {
        [DiscordClient] DiscordClient Client;

        protected override void LoadDefaultConfig()
        {
            LogWarning("Creating a new configuration file");
            Config["Token"] = "DISCORD_TOKEN";
            Config["Format"] = "{current}/{max}";
            Config["Refresh rate"] = 60;
        }

        private void OnServerInitialized()
        {
            var token = Config["Token"].ToString();
            if (token == "DISCORD_TOKEN")
            {
                PrintError("You did not setup your Discord token in the config file!");
                return;
            }
            try
            {
                Discord.CreateClient(this, token);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to initialize Discord Bot, error: {ex.Message}");
            }
            var refreshRate = Config["Refresh rate"].ToString();
            timer.Every(Convert.ToInt32(refreshRate), () =>
            {
                Client.UpdateStatus(new Ext.Discord.DiscordObjects.Presence()
                {
                    Game = new Ext.Discord.DiscordObjects.Game
                    {
                        Name = Config["Format"].ToString().Replace("{current}", players.Connected.Count().ToString()).Replace("{max}", server.MaxPlayers.ToString()),
                        Type = Ext.Discord.DiscordObjects.ActivityType.Game
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
