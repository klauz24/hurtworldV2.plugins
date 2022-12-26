using System.Collections.Generic;

namespace Oxide.Plugins
{

    [Info("Inventory Inspect", "klauz24", "1.0.1")]
    internal class InventoryInspect : HurtworldPlugin
    {
        private class QueueItem
        {
            public ItemObject Item;
            public int Slot;
        }

        [ChatCommand("inspect")]
        private void InspectCommand(PlayerSession session, string command, string[] args)
        {
            if (session.IsAdmin)
            {
                if (args.Length == 1)
                {
                    var targetIPlayer = covalence.Players.FindPlayer(args[0].ToLower());
                    if (targetIPlayer != null)
                    {
                        var targetSession = targetIPlayer.Object as PlayerSession;
                        var queue = new List<QueueItem>();
                        var targetInv = targetSession.WorldPlayerEntity.GetComponent<PlayerInventory>();
                        var recieverInv = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                        for (var i = 0; i < targetInv.Capacity; i++)
                        {
                            var item = targetInv.GetSlot(i);
                            if (item == null)
                            {
                                continue;
                            }
                            queue.Add(new QueueItem()
                            {
                                Item = GlobalItemManager.Instance.CloneItem(item),
                                Slot = i
                            });
                        }
                        recieverInv.ClearItems();
                        foreach (var item in queue)
                        {
                            recieverInv.GiveItemServer(item.Item, item.Slot);
                        }
                        hurt.SendChatMessage(session, "<color=yellow>[Inventory Inspect]</color>", $"Copied inventory of {targetSession.Identity.Name}.");
                    }
                    else
                    {
                        hurt.SendChatMessage(session, "<color=yellow>[Inventory Inspect]</color>", $"Failed to find {args[0]}.");
                    }


                }
                else
                {
                    hurt.SendChatMessage(session, "<color=yellow>[Inventory Inspect]</color>", "Syntax: /inspect <playerName>.");
                }
            }
        }

        [ChatCommand("clear")]
        private void ClearCommand(PlayerSession session, string command, string[] args)
        {
            if (session.IsAdmin)
            {
                session.WorldPlayerEntity.GetComponent<PlayerInventory>().ClearItems();
                hurt.SendChatMessage(session, "<color=yellow>[Inventory Inspect]</color>", "Inventory cleared.");
            }
        }
    }
}