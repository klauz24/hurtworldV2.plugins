using uLink;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Rad Shack", "klauz24", "1.1.4")]
    internal class RadShack : HurtworldPlugin
    {
        private Vector3 _pos;
        private Vector3 _chestSpawnPoint;
        private DateTime _dt;
        private DoorSingleServer _shackDoor;
        private PlayerSession _session;

        private List<GameObject> _gameObjects = new List<GameObject>();
        private List<MapMarkerData> _markerData = new List<MapMarkerData>();
        private List<Vector3> _spawnPoints = new List<Vector3>();


        private class RadShack_Item
        {
            public string Guid;
            public int Min;
            public int Max;
        }

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Door unlock time (in seconds)")]
            public int DoorUnlockTime = 30;

            [JsonProperty(PropertyName = "Event interval")]
            public int EventInterval = 3600;

            [JsonProperty(PropertyName = "Explosives amount of rolls")]
            public int ExplosivesAmountOfRolls = 1;

            [JsonProperty(PropertyName = "Resources amount of rolls")]
            public int ResourcesAmountOfRolls = 3;

            [JsonProperty(PropertyName = "Others amount of rolls")]
            public int OthersAmountOfRolls = 1;

            [JsonProperty(PropertyName = "Event started message")]
            public string EventStartedMessage = "<color=orange>RadShack:</color> event has started!";

            [JsonProperty(PropertyName = "Event wait message")]
            public string EventWaitMessage = "<color=orange>RadShack:</color> someone is trying to find the key!";

            [JsonProperty(PropertyName = "Explosives")]
            public List<RadShack_Item> Explosives = new List<RadShack_Item>();

            [JsonProperty(PropertyName = "Resources")]
            public List<RadShack_Item> Resources = new List<RadShack_Item>();

            [JsonProperty(PropertyName = "Others")]
            public List<RadShack_Item> Others = new List<RadShack_Item>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }
                SaveConfig();
            }
            catch
            {
                PrintWarning("Could not load a valid configuration file, creating a new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        [ChatCommand("radshack.start")]
        private void RadShackStart(PlayerSession session)
        {
            if (session.IsAdmin)
            {
                StartRadShackEvent();
            }
        }

        [ChatCommand("radshack.add")]
        private void RadShackAdd(PlayerSession session)
        {
            if (session.IsAdmin)
            {
                var loc = session.WorldPlayerEntity.transform.position;
                //Server.Broadcast($"Added: {loc.x} {loc.y} {loc.z}");
                _spawnPoints.Add(loc);

                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("RadShackSpawnPoints", _spawnPoints);
                hurt.SendChatMessage(session, null, "<color=lime>Added new Rad Shack spawn point.</color>");
            }
        }

        private void OnServerInitialized()
        {
            _spawnPoints = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<Vector3>>("RadShackSpawnPoints");
            timer.Every(_config.EventInterval, () => StartRadShackEvent());
        }

        private void Unload()
        {
            _shackDoor = null;
            _session = null;
            PurgeEventObjects();
            PurgeMarkerCache();
        }

        private object CanUseSingleDoor(PlayerSession session, DoorSingleServer door)
        {
            if (door is ShackDoorServer && door == _shackDoor)
            {
                if (session == _session)
                {
                    if (_dt > DateTime.Now)
                    {
                        var timeLeft = _dt.Subtract(DateTime.Now);
                        hurt.SendChatMessage(session, null, string.Format(_config.EventWaitMessage, $"{timeLeft.Minutes}m. {timeLeft.Seconds}sec."));
                    }
                    else
                    {
                        UnlockTheDoor(door);
                        HandleMarker(new Color(0, 1, 0, 0.2f), _pos, $"Rad.Shack opened by\n{session.Identity.Name}", "b103a49eb66935a4ab08c236dcae21a2", false);
                        SpawnChestWithLoot(_chestSpawnPoint);
                        _shackDoor = null;
                        timer.Once(60f, () => Unload());
                    }
                }
                else
                {
                    _dt = DateTime.Now.AddSeconds(_config.DoorUnlockTime);
                    _session = session;
                    HandleMarker(new Color(1, 0.92f, 0.016f, 0.2f), _pos, $"Rad.Shack being opened by\n{session.Identity.Name}", "b103a49eb66935a4ab08c236dcae21a2", true);
                }
                return true;
            }
            return null;
        }

        private void StartRadShackEvent()
        {
            Unload();
            _pos = RandomPosition();
            RaycastHit hitInfo;
            Physics.Raycast(_pos, Vector3.down, out hitInfo, float.MaxValue, LayerMaskManager.TerrainConstructionsMachines);
            {
                _pos = Ground(hitInfo);
                if (!IsSafeLocation(hitInfo.point))
                {
                    StartRadShackEvent();
                    return;
                }
                Server.Broadcast(_config.EventStartedMessage);
                HandleMarker(new Color(1, 0, 0, 0.2f), _pos, "Rad. Shack", "b103a49eb66935a4ab08c236dcae21a2", false);
                var shack = SpawnObject(FindNIC("ShackDynamicConstructed"), _pos, Quaternion.identity);
                shack.TryDestroyComponent<ShackDynamicServer>();
                shack.TryDestroyComponent<EntityStats>();
                _gameObjects.Add(shack);
                _shackDoor = shack.GetComponent<DoorSingleServer>();
                _chestSpawnPoint = _pos;
                var ultranium = SpawnObject(FindNIC("UltraniumResourceNodeServer"), _pos + new Vector3(0, -1f, 0), Quaternion.identity);
                ultranium.GetComponent<DestroyInTime>().DestroyDelay = 99999999f;
                _gameObjects.Add(ultranium);
            }
        }

        private void SpawnChestWithLoot(Vector3 position)
        {
            var chest = SpawnObject(FindNIC("StorageChestDynamicConstructed"), position, Quaternion.identity);
            _gameObjects.Add(chest);
            var inv = chest.gameObject.GetComponent<Inventory>();
            for (var i = 0; i < _config.ExplosivesAmountOfRolls; i++)
            {
                AddItem(inv, _config.Explosives);
            }
            for (var i = 0; i < _config.ResourcesAmountOfRolls; i++)
            {
                AddItem(inv, _config.Resources);
            }
            for (var i = 0; i < _config.OthersAmountOfRolls; i++)
            {
                AddItem(inv, _config.Others);
            }
            inv.Invalidate();
        }

        protected void HandleMarker(Color color, Vector3 position, string label, string markerGuid, bool shouldUseTimer = false)
        {
            PurgeMarkerCache();
            if (shouldUseTimer)
            {
                var marker = new MapMarkerData
                {
                    Prefab = RuntimeHurtDB.Instance.GetObjectByGuid(markerGuid).Object as GameObject,
                    Global = true,
                    Color = color,
                    Scale = new Vector3(350f, 350f, 0f),
                    ShowInCompass = true,
                    Position = position,
                    Label = label,
                    ExpireTime = (float)NetworkTime.serverTime + _config.DoorUnlockTime
                };
                MapManagerServer.Instance.RegisterMarker(marker);
                _markerData.Add(marker);
            }
            else
            {
                var marker = new MapMarkerData
                {
                    Prefab = RuntimeHurtDB.Instance.GetObjectByGuid(markerGuid).Object as GameObject,
                    Global = true,
                    Color = color,
                    Scale = new Vector3(350f, 350f, 0f),
                    ShowInCompass = true,
                    Position = position,
                    Label = label
                };
                MapManagerServer.Instance.RegisterMarker(marker);
                _markerData.Add(marker);
            }
        }

        private void PurgeMarkerCache()
        {
            foreach (var marker in _markerData)
            {
                MapManagerServer.Instance.DeregisterMarker(marker);
            }
        }

        private void UnlockTheDoor(DoorSingleServer door)
        {
            door.DoorCollider.enabled = false;
            door.RPC("DOP", uLink.RPCMode.OthersBuffered, new object[] { true, NetworkTime.serverTime });
            door.IsOpen = true;
        }

        private void PurgeEventObjects()
        {
            foreach (var go in _gameObjects)
            {
                HNetworkManager.Instance.NetDestroy(go?.HNetworkView());
            }
        }

        private NetworkInstantiateConfig FindNIC(string obj)
        {
            var networkConfigs = Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>();
            foreach (var netConfig in networkConfigs)
            {
                if (netConfig != null && netConfig.name == obj)
                {
                    return netConfig;
                }
            }
            return null;
        }

        private bool HasCellAuthorization(Vector3 position)
        {
            var cell = ConstructionUtilities.GetOwnershipCell(position);
            if (cell >= 0)
            {
                OwnershipStakeServer stake;
                ConstructionManager.Instance.OwnershipCells.TryGetValue(cell, out stake);
                if (stake != null)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSafeLocation(Vector3 pos)
        {
            if (HasCellAuthorization(pos)) return false;
            var capsuleBottom = pos + ((10f - .75f + 4f) * Vector3.up);
            var capsuleTop = pos + ((10f + .75f - 4f) * Vector3.up);
            if (Physics.CheckCapsule(capsuleBottom, capsuleTop, 4f, LayerMaskManager.TerrainConstructionsMachines, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            return !PhysicsHelpers.IsInRock(pos + Vector3.up * 10f);
        }

        private RadShack_Item GetRandomEntry(List<RadShack_Item> list)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }
            var index = new System.Random().Next(list.Count);
            return list[index];
        }

        private Vector3 RandomPosition()
        {
            //return new Vector3(UnityEngine.Random.Range(-3000f, 3000f), 400f, UnityEngine.Random.Range(-1100f, 1100f));
            //return new Vector3(UnityEngine.Random.Range(-5000f, 5000f), 400f, UnityEngine.Random.Range(-500f, 500f));
            var rnd = new System.Random();
            var next = rnd.Next(_spawnPoints.Count);
            var loc = _spawnPoints[next];
            //Server.Broadcast($"Picked: {loc.x} {loc.y} {loc.z}");
            return loc;
        }

        private Vector3 Ground(RaycastHit hitInfo)
        {
            Vector3 loc = hitInfo.point;
            RaycastHit hit;
            if (Physics.Raycast(loc, Vector3.down, out hit, float.MaxValue, LayerMaskManager.TerrainConstructionsMachines))
            {
                return new Vector3(hit.point.x, hit.point.y + 3.2f, hit.point.z);
            }
            return loc;
        }

        private void AddItem(Inventory inv, List<RadShack_Item> list)
        {
            var item = GetRandomEntry(list);
            if (item == null)
            {
                PrintError($"Failed to get the item at AddItem.");
                return;
            }
            var io = GlobalItemManager.Instance.CreateItem(RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(item.Guid), UnityEngine.Random.Range(item.Min, item.Max));
            inv.GiveItemServer(io);
        }

        private GameObject SpawnObject(NetworkInstantiateConfig config, Vector3 pos, Quaternion quaternion) => HNetworkManager.Instance.NetInstantiate(config, pos, quaternion, GameManager.GetSceneTime());
    }
}
