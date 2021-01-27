using System;

namespace Oxide.Plugins
{
    [Info("Raid Time Limit (Legacy)", "klauz24", "1.0.0")]
    internal class RaidTimeLimit : HurtworldPlugin
    {
        protected override void LoadDefaultConfig()
        {
            Config["LockTime"] = 2;
            Config["UnlockTime"] = 8;
            Config["Message"] = "<color=red>It is not allowed to raid between 02:00 and 08:00!</color>";
        }

        private void OnEntitySpawned(NetworkViewData data)
        {
            if (data.PrefabId == 20 || data.PrefabId == 65)
            {
                Execute(data, false);
            }
            if (data.PrefabId == 85)
            {
                Execute(data, true);
            }
        }

        private void Execute(NetworkViewData data, bool direct)
        {
            if (DateTime.Now.Hour >= int.Parse(Config["LockTime"].ToString()) && DateTime.Now.Hour < int.Parse(Config["UnlockTime"].ToString()))
            {
                if (direct)
                {
                    var session = GameManager.Instance.GetSession(data.NetworkView.owner);
                    hurt.SendChatMessage(session, null, Config["Message"].ToString());
                    Destroy(data.NetworkView);
                }
                else
                {
                    Destroy(data.NetworkView);
                }
            }
        }

        private void Destroy(uLink.NetworkView view) => HNetworkManager.Instance.NetDestroy(view);
    }
}
