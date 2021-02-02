using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

/* 
 * Modified version to work with KillFeed plugin.
 * KillFeed: https://github.com/klauz24/Plugins.Hurtworld/blob/main/KillFeed.cs
 */
namespace Oxide.Plugins
{
    [Info("KillCounter", "Mr. Blue", "2.0.3")]
    [Description("Creates a kill count for each player. Displays on the death notice.")]

    class KillCounter : HurtworldPlugin
    {
        #region Variables
        private static readonly string UsePerm = "killcounter.use";
        private static readonly string AdminPerm = "killcounter.admin";

        public static bool sameStake = false;
        public static bool sameClan = false;

        private static Dictionary<ulong, int> data = new Dictionary<ulong, int>();
        #endregion Variables



        #region Methods
        protected override void LoadDefaultConfig()
        {
            if (Config["ResetOnLoad"] == null) Config.Set("ResetOnLoad", false);
            if (Config["SameStake"] == null) Config.Set("SameStake", true);
            if (Config["SameClan"] == null) Config.Set("SameClan", true);
            SaveConfig();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "no_permission", "You don't have permission to use this command.\nRequired: <color=orange>{perm}</color>." },
                { "player", "{Name} now has {Kills} kills." },
                { "playerkills", "You have {Kills} kill(s)!" },
                { "playernokills", "You have no kills!" },
                { "playertop", "========Top 5 players========" },
                { "playertopline", "{Number}. {Name} has {Kills} kills." },
                { "kc_reset_all", "All players kill count have been reseted." },
                { "kc_commands_list", "<color=yellow>Available KillCounter Commands</color>" },
                { "kc_cmds", "<color=orange>/kc top</color> - See top 5 killers.\n<color=orange>/kc kills</color> - See the amount of kills you made." },
                { "kc_admincmds", "Admin commands:\n<color=orange>/kc resetall</color> - Resets all players kill count." }
            }, this);
        }

        private void LoadData()
        {
            if (!(bool)Config["ResetOnLoad"])
                data = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, int>>("KillCounter");
            else
            {
                data = new Dictionary<ulong, int>();
                SaveData();
            }
        }

        private void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("KillCounter", data);
        }

        void Init()
        {
            LoadData();
            sameStake = (bool)Config["SameStake"];
            sameClan = (bool)Config["SameClan"];
            permission.RegisterPermission(AdminPerm, this);
            permission.RegisterPermission(UsePerm, this);
        }

        void Unload() => SaveData();
        void OnServerSave() => SaveData();

        private bool IsValidKill(PlayerSession victim, PlayerSession killer)
        {
            if (sameStake)
            {
                var stakes = Resources.FindObjectsOfTypeAll<OwnershipStakeServer>();
                foreach (OwnershipStakeServer stake in stakes)
                {
                    if (stake.AuthorizedPlayers.Contains(victim.Identity) && stake.AuthorizedPlayers.Contains(killer.Identity))
                        return false;
                }
            }

            if (sameClan)
            {
                Clan victim_clan = victim.Identity.Clan;
                Clan killer_clan = killer.Identity.Clan;

                if (victim_clan != null && killer_clan != null)
                    if (victim_clan.Equals(killer_clan))
                        return false;
            }

            return true;
        }
        private PlayerSession GetPlayerSession(EntityEffectSourceData dataSource)
        {
            if (dataSource?.EntitySource?.GetComponent<EntityStats>()?.networkView == null) return null;
            HNetworkView networkView = dataSource.EntitySource.GetComponent<EntityStats>().networkView;
            return GameManager.Instance.GetSession(networkView.owner);
        }
        #endregion Methods

        #region Chat Commands
        [ChatCommand("kc")]
        void cmdKC(PlayerSession session, string command, string[] args)
        {
            string steamId = session.SteamId.ToString();
            if (!permission.UserHasPermission(steamId, UsePerm))
            {
                Player.Message(session, lang.GetMessage("no_permission", this, steamId)
                    .Replace("{perm}", UsePerm));
                return;
            }

            if (args.Length > 0 && args[0].ToLower() == "top")
            {
                Player.Message(session, lang.GetMessage("playertop", this, steamId));
                IEnumerable<KeyValuePair<ulong, int>> topPlayerKills = data.OrderByDescending(pair => pair.Value).Take(5);

                int index = 1;
                foreach (KeyValuePair<ulong, int> playerKills in topPlayerKills)
                {
                    string playerName = "Unknown";
                    IPlayer player = covalence.Players.FindPlayerById(playerKills.Key.ToString());

                    if (player != null)
                        playerName = player.Name;

                    Player.Message(session, lang.GetMessage("playertopline", this, steamId)
                        .Replace("{Number}", index.ToString())
                        .Replace("{Name}", playerName)
                        .Replace("{Kills}", playerKills.Value.ToString()));
                    index++;
                }
                return;
            }

            if (args.Length > 0 && args[0].ToLower() == "kills")
            {
                if (!data.ContainsKey(session.SteamId.m_SteamID))
                    Player.Message(session, lang.GetMessage("playernokills", this, steamId));
                else
                {
                    var kills = data[session.SteamId.m_SteamID];
                    Player.Message(session, lang.GetMessage("playerkills", this, steamId)
                        .Replace("{Kills}", kills.ToString()));
                }
                return;
            }

            if (!permission.UserHasPermission(steamId, AdminPerm) && args.Length > 0 && args[0].ToLower() == "resetall")
            {
                Player.Message(session, lang.GetMessage("no_permission", this, steamId)
                    .Replace("{perm}", AdminPerm));
                return;
            }
            else if (permission.UserHasPermission(steamId, AdminPerm) && args.Length > 0 && args[0].ToLower() == "resetall")
            {
                data = new Dictionary<ulong, int>();
                SaveData();
                Player.Message(session, lang.GetMessage("kc_reset_all", this, steamId));
            }
            else
            {
                Player.Message(session, lang.GetMessage("kc_commands_list", this, steamId));
                Player.Message(session, lang.GetMessage("kc_cmds", this, steamId));
                if (permission.UserHasPermission(steamId, AdminPerm))
                    Player.Message(session, lang.GetMessage("kc_admincmds", this, steamId));
            }
        }
        #endregion Chat Commands

        #region Hooks
        string AddKill(PlayerSession victim, EntityEffectSourceData dataSource)
        {
            PlayerSession killer = GetPlayerSession(dataSource);
            if (killer == null) return null;

            ulong killer_steamID = killer.SteamId.m_SteamID;

            if (!data.ContainsKey(killer_steamID))
                data.Add(killer_steamID, 0);

            if (IsValidKill(killer, victim))
                data[killer_steamID] += 1;

            return data[killer_steamID].ToString() ?? null;
        }
        #endregion Hooks
    }
}