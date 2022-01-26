using Oxide.Ext.Discord;
using System.Collections.Generic;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Entities.Activities;
using Oxide.Ext.Discord.Entities.Gatway.Commands;

// Latest Discord extension: https://umod.org/extensions/discord

namespace Oxide.Plugins
{
    [Info("Player Count Discord Bot", "klauz24", "1.0.2")]
    internal class PlayerCountDiscordBot : CovalencePlugin
    {
        [DiscordClient] private DiscordClient _client;

        private DiscordActivity _activity = new DiscordActivity();

        private UpdatePresenceCommand _status = new UpdatePresenceCommand
        {
            Afk = false,
            Since = 0,
            Status = UserStatusType.Online
        };

        protected override void LoadDefaultConfig()
        {
            Config["UpdateInterval"] = 10f;
            Config["Token"] = "";
            Config["Status"] = "{0}/{1}";
        }

        private void OnServerInitialized() => TryToStart();

        private void TryToStart()
        {
            var token = Config["Token"].ToString();
            if (token == "")
            {
                PrintError("Discord token is not setup!");
                return;
            }
            var settings = new DiscordSettings { ApiToken = token };
            _client.Connect(settings);
            _status.Activities = new List<DiscordActivity>
            {
                _activity
            };
            var updateInterval = float.Parse(Config["UpdateInterval"].ToString());
            timer.Every(updateInterval, () =>
            {
                _activity.Name = string.Format(Config["Status"].ToString(), server.Players, server.MaxPlayers);
                _activity.Type = ActivityType.Game;
                _client?.Bot?.UpdateStatus(_status);
            });
        }
    }
}
