using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
	[Info("Custom Town Loot", "klauz24", "1.0.0")]
	internal class CustomTownLoot : HurtworldPlugin
	{
		private List<IItem> _list = new List<IItem>();

		private class IItem
		{
			public string Note;
			public string Guid;
			public int MinStack;
			public int MaxStack;
		}

		private void OnServerInitialized()
		{
			_list = Interface.uMod.DataFileSystem.ReadObject<List<IItem>>("CustomTownLoot");
			if (_list.Count == 0)
			{
				PrintWarning("Custom town loot list is empty, adding Owrong as example.");
				var owrong = new IItem()
				{
					Note = "Just a note here for easier filtering once list gets bigger. (Example: Owrong)",
					Guid = "2e718220fde28dd4d8ec5ef1c101a9e2",
					MinStack = 1,
					MaxStack = 1
				};
				_list.Add(owrong);
				Interface.uMod.DataFileSystem.WriteObject("CustomTownLoot", _list);
			}
		}
		
	        private void OnEntitySpawned(HNetworkView data)
		{
			if (data.gameObject.name == "GenericTownLootCacheServer(Clone)")
			{
				var inv = data.gameObject.GetComponent<Inventory>();
				if (inv != null)
				{
					inv.ClearItems();
					AddCustomItem(inv);
				}
			}
		}

		private void AddCustomItem(Inventory inv)
                {
			var rnd = new System.Random();
			var index = rnd.Next(_list.Count);
			var item = _list[index];
			var newItem = GlobalItemManager.Instance.CreateItem(RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(item.Guid), rnd.Next(item.MinStack, item.MaxStack));
			inv.GiveItemServer(newItem);
		}
	}
}
