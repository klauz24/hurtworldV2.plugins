using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("Kill Feed", "klauz24", "1.0.2")]
	internal class KillFeed : HurtworldPlugin
	{
        [PluginReference] private Plugin KillCounter;

        private bool _isKillCounterInstalled;

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                // PvP
                {"PvPKill", "{0} has been killed by {1} with {2} in {3} by {4} meters"},
                {"PvPKilWithKillCounter", "{0} has been killed by {1} ({2}) with {3} in {4} by {5} meters"},
                // PvE
                {"Creatures/Antor", "{0} got killed by a Antor"},
                {"Creatures/Bandrill", "{0} got killed by a Bandrill"},
                {"Creatures/Bor", "{0} got killed by a Bor"},
                {"Creatures/DartBug", "{0} got killed by a Dart Bug"},
                {"Creatures/Radiation Bor", "{0} got killed by a Radiation Bor"},
                {"Creatures/Rafaga", "{0} got killed by a Rafaga"},
                {"Creatures/Sabra", "{0} got killed by a Sabra"},
                {"Creatures/Sasquatch", "{0} got killed by a Sasquatch"},
                {"Creatures/Skoogler", "{0} got killed by a Skoogler"},
                {"Creatures/Shigi", "{0} got killed by a Shigi"},
                {"Creatures/Tokar", "{0} got killed by a Tokar"},
                {"Creatures/Thornling", "{0} got killed by a Thornling"},
                {"Creatures/Yeti", "{0} got killed by a Yeti"},
                {"EntityStats/BinaryEffects/Asphyxiation", "{0} has died from suffocation"},
                {"EntityStats/BinaryEffects/Burning", "{0} has burned to death"},
                {"EntityStats/BinaryEffects/Drowning", "{0} has drowned"},
                {"EntityStats/BinaryEffects/Hyperthermia", "{0} has died from overheating"},
                {"EntityStats/BinaryEffects/Hypothermia", "{0} has frozen to death"},
                {"EntityStats/BinaryEffects/Radiation Poisoning", "{0} has died from radiation poisoning"},
                {"EntityStats/BinaryEffects/Starvation", "{0} has starved to death"},
                {"EntityStats/BinaryEffects/Starving", "{0} has starved to death"},
                {"EntityStats/BinaryEffects/Territory Control Lockout Damage", "{0} got killed by Territory Control Lockout Damage"},
                {"EntityStats/Sources/Damage Over Time", "{0} just died"},
                {"EntityStats/Sources/Explosives", "{0} got killed by an explosion"},
                {"EntityStats/Sources/Fall Damage", "{0} has fallen to their death"},
                {"EntityStats/Sources/Poison", "{0} has died from poisoning"},
                {"EntityStats/Sources/Radiation", "{0} has died from radiation"},
                {"EntityStats/Sources/Suicide", "{0} has committed suicide"},
                {"EntityStats/Sources/a Vehicle Impact", "{0} got run over by a vehicle"},
                {"Machines/Landmine", "{0} got killed by a Landmine"},
                {"Machines/Medusa Vine", "{0} got killed by a Medusa Trap"},
                {"Too Cold", "{0} has frozen to death"},
                {"Unknown", "Unknown"}
            }, this);
        }

        private void OnServerInitialized()
        {
            GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = false;
            if (KillCounter != null)
            {
                _isKillCounterInstalled = true;
                Puts("KillCounter has been detected.");
            }
            else
            {
                _isKillCounterInstalled = false;
                Puts("KillCounter wasn't detected.");
            }
        }

        private void Unload() => GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = true;

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            var attacker = GetPlayerSession(source);
            if (attacker == null)
            {
                var langKey = GetLang(session, source.SourceDescriptionKey);
                if (langKey != null)
                {
                    BroadcastInChat(langKey, session.Identity.Name);
                }
                else
                {
                    Puts("Detected unknown lang key: ", source.SourceDescriptionKey);
                }
            }
            else
            {
                var hitbox = GetHitbox(source);
                var distance = GetDistance(session, attacker);
                var weaponName = GetWeaponName(attacker);
                if (_isKillCounterInstalled)
                {
                    var killCounter = KillCounter.Call("AddKill", session, source);
                    BroadcastInChat("PvPKilWithKillCounter", session.Identity.Name, attacker.Identity.Name, killCounter, weaponName, hitbox, distance);
                }
                else
                {
                    BroadcastInChat("PvPKill", session.Identity.Name, attacker.Identity.Name, weaponName, hitbox, distance);
                }
            }
        }

        private void BroadcastInChat(string str, params object[] args)
        {
            foreach(var session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null)
                {
                    hurt.SendChatMessage(session, null, string.Format(GetLang(session, str), args));
                }
            }
        }

        private string GetWeaponName(PlayerSession session)
        {
            var handler = session.WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
            if (handler != null)
            {
                var equippedItem = handler.GetEquippedItem();
                if (equippedItem != null)
                {
                    var weaponName = equippedItem.GetDataProvider().NameKey.Split('/').Last();
                    if (weaponName != null)
                    {
                        return weaponName;
                    }
                }
            }
            return GetLang(session, "Unknown");
        }

        private PlayerSession GetPlayerSession(EntityEffectSourceData source)
        {
            if (source != null && source.EntitySource != null)
            {
                var view = source.EntitySource.GetComponent<HNetworkView>();
                if (view != null)
                {
                    var session = GameManager.Instance.GetSession(view.owner);
                    if (session != null)
                    {
                        return session;
                    }
                }
            }
            return null;
        }

        private string GetDistance(PlayerSession session, PlayerSession attacker)
        {
            var posVictim = session.WorldPlayerEntity.transform.position;
            var posAttacker = attacker.WorldPlayerEntity.transform.position;
            if (posVictim != null && posAttacker != null)
            {
                var distance = Vector3.Distance(posVictim, posAttacker).ToString().Split('.').First();
                if (distance != null)
                {
                    return distance;
                }
            }
            return GetLang(session, "Unknown");
        }

        private string GetHitbox(EntityEffectSourceData source) => source.Hitbox.ToString();

        private string GetLang(PlayerSession session, string langKey) => lang.GetMessage(langKey, this, session.SteamId.ToString());
    }
}