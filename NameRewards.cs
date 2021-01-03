using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Name Rewards", "klauz24", "1.0.0")]
    internal class NameRewards : CovalencePlugin
    {
        private const string _pluginPerm = "namerewards.use";

        protected override void LoadDefaultConfig()
        {
            Config["Words"] = new List<string>()
            {
                "SomeWordHereForRewards"
            };
        }

        private void Init() => permission.RegisterPermission(_pluginPerm, this);

        private void OnUserConnected(IPlayer player)
        {
            foreach(var str in Config["Words"] as List<string>)
            {
                var match = player.Name.ToLower().Contains(str.ToLower());
                if (match && !permission.UserHasPermission(player.Id, _pluginPerm))
                {
                    permission.GrantUserPermission(player.Id, _pluginPerm, this);
                    Puts($"Granting plugin permission to {player.Name}");
                }
                else
                {
                    if (permission.UserHasPermission(player.Id, _pluginPerm))
                    {
                        permission.RevokeUserPermission(player.Id, _pluginPerm);
                        Puts($"Revoking plugin permission from {player.Name}");
                    }
                }
            }
        }
    }
}