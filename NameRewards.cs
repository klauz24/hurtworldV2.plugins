using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Name Rewards", "klauz24", "1.0.1")]
    internal class NameRewards : CovalencePlugin
    {
        private const string _perm = "namerewards.use";

        private PluginConfig _config;

        private class PluginConfig
        {
            [JsonProperty("Words")]
            public List<string> Words;
        }

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig()
            { 
                Words = new List<string>() { "SomeWordHere" }
            };
        }

        private void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
            permission.RegisterPermission(_perm, this);
        }

        private void OnUserConnected(IPlayer player)
        {
            foreach (var str in _config.Words)
            {
                var match = player.Name.ToLower().Contains(str.ToLower());
                if (match && !permission.UserHasPermission(player.Id, _perm))
                {
                    permission.GrantUserPermission(player.Id, _perm, this);
                }
                else
                {
                    if (permission.UserHasPermission(player.Id, _perm))
                    {
                        permission.RevokeUserPermission(player.Id, _perm);
                    }
                }
            }
        }
    }
}
