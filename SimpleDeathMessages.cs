using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Simple Death Messages", "klauz24", "1.0.0")]
	internal class SimpleDeathMessages : HurtworldPlugin
    {
        protected override void LoadDefaultConfig()
        {
            Config["PvE Message"] = "<color=lime>{0}</color> got killed by <color=green>{1}</color>";
            Config["PvP Message"] = "<color=lime>{0}</color> got killed by <color=green>{1}</color> with {2} from {3}m";
        }

        private void OnServerInitialized() => GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = false;

        private void Unload() => GameManager.Instance.ServerConfig.ChatDeathMessagesEnabled = true;

        private void OnPlayerDeath(PlayerSession session, EntityEffectSourceData source)
        {
            if (session != null && source?.EntitySource != null)
            {
                var attacker = GetPlayerSession(source);
                if (attacker != null)
                {
                    var weaponName = GetWeaponName(attacker);
                    var distance = GetDistance(session.WorldPlayerEntity.transform.position, attacker.WorldPlayerEntity.transform.position).ToString().Split('.').First();
                    GlobalBroadcast(Config["PvP Message"].ToString(), session.Identity.Name, attacker.Identity.Name, weaponName, distance);
                }
                else
                {
                    var entityName = source.SourceDescriptionKey.Split('/').Last();
                    GlobalBroadcast(Config["PvE Message"].ToString(), session.Identity.Name, entityName);
                }
            }
        }

        private void GlobalBroadcast(string messageString, params object[] args)
        {
            foreach(var session in GameManager.Instance.GetSessions().Values)
            {
                if (session != null)
                {
                    hurt.SendChatMessage(session, null, string.Format(messageString, args));
                }
            }
        }

        private string GetWeaponName(PlayerSession session)
        {
            var handler = session.WorldPlayerEntity.GetComponent<EquippedHandlerServer>();
            if (handler != null)
            {
                return handler.GetEquippedItem().GetDataProvider().NameKey.Split('/').Last();
            }
            return null;
        }

        private PlayerSession GetPlayerSession(EntityEffectSourceData source)
        {
            var stats = source.EntitySource.GetComponent<EntityStats>();
            if (stats != null)
            {
                var view = stats.GetComponent<HNetworkView>();
                if (view != null)
                {
                    return GameManager.Instance.GetSession(view.owner);
                }
            }
            return null;
        }

        private float GetDistance(Vector3 pos0, Vector3 pos1) => Vector3.Distance(pos0, pos1);
    }
}
