using Assets.Scripts;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine.EventSystems;

namespace Oxide.Plugins
{
    [Info("MeteorsV2", "Kao", "0.0.1")]
    [Description("MeteorsV2")]
    
    class MeteorsV2 : HurtworldPlugin
    {
        static MeteorsV2 _plugin = null;
        void OnServerInitialized()
        {
            _plugin = this;
            LoadedTimers();
        }
        void LoadedTimers()
        {
            foreach (var time in config.BeforeSpawnMessage)
            {
                if (config.TimeToSpawn - time.Key < 0) continue;
                timer.Once(config.TimeToSpawn - time.Key + 0.01f, () => Server.Broadcast(time.Value));
                timer.Once(config.TimeToSpawn - time.Key + 0.01f, () => timer.Repeat(config.TimeToSpawn, 0, () => Server.Broadcast(time.Value)));
            }
            Meteor.TimeToNext = config.TimeToSpawn;
            timer.Repeat(1f, 0, () =>
            {
                try
                {
                    if (Meteor.TimeToNext <= 0)
                    {
                        SpawnMeteor();
                        Meteor.TimeToNext = config.TimeToSpawn;
                    }
                }
                catch
                {

                }
                Meteor.DelTimeToNext();
            });
        }
        void SpawnMeteor()
        {
            RaycastHit ray;
            if (config.SpawnInPos)
            {
                if (config.SpawnInRND)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        if (Physics.Raycast(new Vector3(Random.Range(-3500f, 3500f), 1000f, Random.Range(-1000f, 1000f)), Vector3.down, out ray, 2000f, LayerMaskManager.TerrainAndConstructions))
                        {
                            SpawnMeteor(ray.point);
                        }
                    }
                    else
                    {
                        if (Physics.Raycast(config.GetPosition(), Vector3.down, out ray, 2000f, LayerMaskManager.TerrainAndConstructions))
                        {
                            SpawnMeteor(ray.point);
                        }
                    }
                }
                else
                {
                    if (Physics.Raycast(config.GetPosition(), Vector3.down, out ray, 2000f, LayerMaskManager.TerrainAndConstructions))
                    {
                        SpawnMeteor(ray.point);
                    }
                }
            }
            else
            {
                if (config.SpawnInRND)
                {
                    if (Physics.Raycast(new Vector3(Random.Range(-3500f, 3500f), 1000f, Random.Range(-1000f, 1000f)), Vector3.down, out ray, 2000f, LayerMaskManager.TerrainAndConstructions))
                    {
                        SpawnMeteor(ray.point);
                    }
                }
                else
                {

                }
            }
        }
        class LootData
        {
            public byte MaxCount;
            public byte MinCount;
            public string GUID;
            public int Rate;
            public LootData(byte maxcount, byte mincount, string guid, int rate)
            {
                this.MaxCount = maxcount;
                this.MinCount = mincount;
                this.GUID = guid;
                this.Rate = rate;
            }
            public virtual void SpawnNode(Vector3 position) => Spawned(RuntimeHurtDB.Instance.GetObjectByGuid<ItemGeneratorAsset>(GUID), Random.Range(MinCount, MaxCount + 1), position);
            public static void Spawn(List<LootData> list, Vector3 position)
            {
                foreach (var item in list)
                    if (item.Rate > Random.Range(0, 101))
                        item.SpawnNode(position);
            }
            public static void Spawned(ItemGeneratorAsset generator, int count, Vector3 position)
            {
                try
                {
                    ItemObject item = GlobalItemManager.Instance.CreateItem(generator, count);
                    WorldItemServer.SpawnWorldItem(item, position + new Vector3(0, 2, 0), new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Error] {e.Message} {e.StackTrace}");
                }
            }
        }
        static ConfigData config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config?.SpawnInPos == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig() => config = new ConfigData();
        protected override void SaveConfig() => Config.WriteObject(config);
        class ConfigData
        {
            public bool SpawnInPos = false;
            public bool SpawnInRND = true;
            public Dictionary<string, string> Positions = new Dictionary<string, string>();
            public Vector3 GetPosition()
            {
                var pos = Positions.ElementAt(Random.Range(0, Positions.Count));
                return new Vector3(Convert.ToSingle(pos.Value.Split(' ')[0]), 1000, Convert.ToSingle(pos.Value.Split(' ')[2]));
            }
            public List<LootData> Loot = new List<LootData>();
            public int TimeToApproach = 300;
            public int TimeToSpawn = 1800;
            public Dictionary<int, string> TimeMessage = new Dictionary<int, string>();
            public Dictionary<int, string> BeforeSpawnMessage = new Dictionary<int, string>();
            public string MessageInCommand_Meteors = "The meteor area is way too hot to approach. It should be colder in {min} minute(s).";
            public string MessageInCommand_ToNext = "Next meteor was falling after {min} minute(s)!";
            public string MessageInMap_Falling = "A meteor is falling";
            public string MessageInMap_Landed = "A meteor landed";
        }
        [ChatCommand("go.meteor")]
        void Go_Meteor_Command(PlayerSession session)
        {
            if (!session.IsAdmin) return;
            SpawnMeteor(session.WorldPlayerEntity.transform.position);
        }
        [ChatCommand("add.meteor")]
        void Add_Meteor_Command(PlayerSession session, string command, string[] args)
        {
            if (!session.IsAdmin) return;
            if (args?.Length != 1)
            {
                Player.Message(session, "/add.meteor [location]");
                return;
            }
            config.Positions.Add(args[0], session.WorldPlayerEntity.transform.position.x + " " + session.WorldPlayerEntity.transform.position.z);
            SaveConfig();
            Player.Message(session, "added meteor location " + args[0]);
        }
        

        [ChatCommand("meteor")]
        void Meteor_Command(PlayerSession session)
        {
            Player.Message(session, Meteor.GetMeteors());
        }

        class Meteor
        {
            public static int TimeToNext = 0;
            public static void DelTimeToNext(int count = 1) => TimeToNext = (TimeToNext - count) <= 0 ? 0 : (TimeToNext - count);
            public static List<Meteor> meteors = new List<Meteor>();
            public static void OnMeteorSpawned(GameObject obj)
            {
                Meteor meteor = new Meteor(obj);
                meteors.Add(meteor);
                meteor.Start();
            }
            public static string GetMeteors() => $"{config.MessageInCommand_ToNext.Replace("{min}", Mathf.FloorToInt(TimeToNext / 60f).ToString())}{((meteors.Count != 0) ? "\nActive meteors:\n" : "")}{string.Join("\n", meteors.Select((m, num) => $"{num + 1}: {m.ToString()}").ToArray())}";

            public GameObject Prefab;
            public MapMarkerData Marker = null;
            public int Count = -1;
            public Meteor(GameObject prefab)
            {
                Prefab = prefab;
            }
            public virtual void Start()
            {
                Prefab.GetComponent<DestroyInTime>().StopAllCoroutines();
                Vector3 position = Prefab.transform.position;
                foreach (var item in config.TimeMessage) _plugin.timer.Once(item.Key, () => _plugin.Server.Broadcast(item.Value));
                _plugin.SpawnTimeMarker(position, new Color(1, 0.27f, 0, 0.75f), config.TimeToApproach, config.MessageInMap_Landed);
                _plugin.timer.Once(config.TimeToApproach + 0.0f, () => HNetworkManager.Instance.NetDestroy(Prefab.HNetworkView()));
                _plugin.timer.Once(config.TimeToApproach + 0.1f, () => _plugin.SpawnBoom(position + new Vector3(0, 0.1f, 0)));
                _plugin.timer.Once(config.TimeToApproach + 1f, () => LootData.Spawn(config.Loot, position + new Vector3(0, 0.2f, 0)));
                Count = config.TimeToApproach;
                _plugin.timer.Repeat(1f, config.TimeToApproach, () => { Count = (Count - 1) <= 0 ? 0 : (Count - 1); });
                _plugin.timer.Once(config.TimeToApproach + 0.3f, () => { meteors.Remove(this); });
            }
            public override string ToString() => config.MessageInCommand_Meteors.Replace("{min}", Mathf.FloorToInt(Count / 60f).ToString());
        }

        void SpawnMeteor(Vector3 pos)
        {
            if (Nodes.GetKey("MeteorImpactNode") == null) { PrintError("Null"); return; }
            GameObject meteor = Singleton<HNetworkManager>.Instance.NetInstantiate(Prefabs.GetKey("MeteorStrikeEvent"), pos, Quaternion.Euler(0, 0, 0), GameManager.GetSceneTime());
            Color RBG = Color.black;
            RBG.a = 50;
            meteor.GetComponent<MeteorEvent>().MapMarker.Color = RBG;
            meteor.GetComponent<MeteorEvent>().MapMarker.Scale = new Vector3(250, 250, 250);
            meteor.GetComponent<MeteorEvent>().MapMarker.Label = config.MessageInMap_Falling;
            var Node = Nodes.GetKey("MeteorImpactNode");
            meteor.GetComponent<MeteorEvent>().ResourceNode = Node;
        }
        void SpawnBoom(Vector3 pos)
        {
            if (Prefabs.GetKey("ExplosionServer") != null)
            {
                ExplosionServer explosion = Singleton<HNetworkManager>.Instance.NetInstantiate(Prefabs.GetKey("ExplosionServer"), pos, Quaternion.identity, GameManager.GetSceneTime()).GetComponent<ExplosionServer>();
                explosion.SetData((from o in Resources.FindObjectsOfTypeAll<ExplosiveDynamicServer>() where o.transform.name.Equals("C4DynamicObject") select o).First().Configuration);
                explosion.LatRows = 10;
                explosion.LongRows = 10;
                explosion.Radius = 10;
                explosion.Explode();
            }
        }
        PlayerSession InPlayer(string name)
        {
            foreach (var pair in GameManager.Instance.GetSessions())
            {
                PlayerSession tplayer = pair.Value;
                if (tplayer.Identity.Name.ToLower().Contains(name.ToLower()))
                    return tplayer;
            }
            return null;
        }
        void OnEntitySpawned(HNetworkView data)
        {
            if (data.gameObject.name.Contains("MeteorImpactNode"))
                Meteor.OnMeteorSpawned(data.gameObject);
        }
        List<MapMarkerData> markers = new List<MapMarkerData>();
        void SpawnTimeMarker(Vector3 position, Color color, float dtime, string message)
        {
            MapMarkerData data = Clone(Resources.FindObjectsOfTypeAll<MapMarkerObject>().Where(obj => obj.name.Contains("MeteorImpactNode")).First().Data);
            data.Color = color;
            data.Global = true;
            data.Position = position;
            data.ShowInCompass = false;
            data.Label = message;
            data.Scale = new Vector3(250, 250, 250);
            data.ExpireTime = (float)uLink.NetworkTime.serverTime + dtime;
            MapManagerServer.Instance.RegisterMarker(data);
            markers.Add(data);
            timer.Once(dtime, () =>
            {
                if (markers.Contains(data))
                    markers.Remove(data);
                MapManagerServer.Instance.DeregisterMarker(data);
            });
        }
        void Unload()
        {
            foreach (var data in markers.ToArray()) MapManagerServer.Instance.DeregisterMarker(data);
            markers.Clear();
        }
        static T Clone<T>(T obj)
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)inst?.Invoke(obj, null);
        }
        struct EEntityFluidEffectTypes
        {
            public static EntityFluidEffectKey GetKey(string name)
            {
                if (KeysData.Count() == 0) LoadKey();
                if (KeysData.ContainsKey((name + " (EntityFluidEffectKey)").ToLower()))
                    return KeysData[(name + " (EntityFluidEffectKey)").ToLower()];
                return null;
            }
            static Dictionary<string, EntityFluidEffectKey> KeysData = new Dictionary<string, EntityFluidEffectKey>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<EntityFluidEffectKey>())
                {
                    if (st != null)
                    {
                        if (!KeysData.ContainsKey((st + "").ToLower()))
                        {
                            KeysData.Add((st + "").ToLower(), st);
                        }
                    }
                }
            }
        }
        struct EffectStats
        {
            public static EntityStats SetValue(EntityStats stats, string Type, float Value)
            {
                if (EEntityFluidEffectTypes.GetKey(Type) == null && DebugEffectStats) Debug.LogError($"EEntityFluidEffectType {Type} not found!");
                if (EEntityFluidEffectTypes.GetKey(Type) == null) return stats;
                ((StandardEntityFluidEffect)stats.GetFluidEffect(EEntityFluidEffectTypes.GetKey(Type))).SetValue(Value);
                return stats;
            }
            public static float GetValue(EntityStats stats, string Type)
            {
                if (EEntityFluidEffectTypes.GetKey(Type) == null && DebugEffectStats) Debug.LogError($"EEntityFluidEffectType {Type} not found!");
                if (EEntityFluidEffectTypes.GetKey(Type) == null) return 0f;
                return ((StandardEntityFluidEffect)stats.GetFluidEffect(EEntityFluidEffectTypes.GetKey(Type))).GetValue();
            }
            public static EntityStats MaxValue(EntityStats stats, string Type, float Value)
            {
                if (EEntityFluidEffectTypes.GetKey(Type) == null && DebugEffectStats) Debug.LogError($"EEntityFluidEffectType {Type} not found!");
                if (EEntityFluidEffectTypes.GetKey(Type) == null) return stats;
                ((StandardEntityFluidEffect)stats.GetFluidEffect(EEntityFluidEffectTypes.GetKey(Type))).MaxValue = Value;
                return stats;
            }
            public static EntityStats MinValue(EntityStats stats, string Type, float Value)
            {
                if (EEntityFluidEffectTypes.GetKey(Type) == null && DebugEffectStats) Debug.LogError($"EEntityFluidEffectType {Type} not found!");
                if (EEntityFluidEffectTypes.GetKey(Type) == null) return stats;
                ((StandardEntityFluidEffect)stats.GetFluidEffect(EEntityFluidEffectTypes.GetKey(Type))).MinValue = Value;
                return stats;
            }
            public static EntityStats ResetValue(EntityStats stats, string Type)
            {
                if (EEntityFluidEffectTypes.GetKey(Type) == null && DebugEffectStats) Debug.LogError($"EEntityFluidEffectType {Type} not found!");
                if (EEntityFluidEffectTypes.GetKey(Type) == null) return stats;
                ((StandardEntityFluidEffect)stats.GetFluidEffect(EEntityFluidEffectTypes.GetKey(Type))).Reset(true);
                return stats;
            }
        }
        static bool DebugEffectStats = false;
        struct Nodes
        {
            public static SpawnObjectBuilder GetKey(string name)
            {
                if (KeysData.Count() < 5) LoadKey();
                if (KeysData.ContainsKey((name + " (spawnobjectbuilder)").ToLower()))
                {
                    return KeysData[(name + " (spawnobjectbuilder)").ToLower()];
                }
                if (KeysData.ContainsKey((name + "").ToLower()))
                {
                    return KeysData[(name + "").ToLower()];
                }
                //Debug.LogError("[" + (name + " (spawnobjectbuilder)").ToLower() + "|null] and  [" + (name + "").ToLower() + "|null]");
                return null;
            }
            static Dictionary<string, SpawnObjectBuilder> KeysData = new Dictionary<string, SpawnObjectBuilder>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<SpawnObjectBuilder>())
                {
                    if (st != null)
                    {
                        if (!KeysData.ContainsKey((st + "").ToLower()))
                        {
                            KeysData.Add((st + "").ToLower(), st);
                            //Debug.LogWarning((st + "").ToLower());
                        }
                    }
                }
            }
        }
        struct MapMarker
        {
            public static MapMarkerObject GetKey(string name)
            {
                if (KeysData.Count() < 1) LoadKey();
                if (KeysData.ContainsKey((name + "(mapmarkerobject)").ToLower()))
                {
                    return KeysData[(name + "(mapmarkerobject)").ToLower()];
                }
                if (KeysData.ContainsKey((name + "").ToLower()))
                {
                    return KeysData[(name + "").ToLower()];
                }
                Debug.LogError("[" + (name + "(mapmarkerobject)").ToLower() + "|null] and  [" + (name + "").ToLower() + "|null]");
                return null;
            }
            static Dictionary<string, MapMarkerObject> KeysData = new Dictionary<string, MapMarkerObject>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<MapMarkerObject>())
                {
                    if (st != null)
                    {
                        if (!(st.name + "").ToLower().Contains("(clone)"))
                        {
                            if (!KeysData.ContainsKey((st.name + "").ToLower()))
                            {
                                KeysData.Add((st.name + "").ToLower(), st);
                            }
                        }
                    }
                }
            }
        }
        struct Prefabs
        {
            public static NetworkInstantiateConfig GetKey(string name)
            {
                if (KeysData.Count() < 5) LoadKey();
                if (KeysData.ContainsKey((name + " (networkinstantiateconfig)").ToLower()))
                {
                    return KeysData[(name + " (networkinstantiateconfig)").ToLower()];
                }
                if (KeysData.ContainsKey((name + "").ToLower()))
                {
                    return KeysData[(name + "").ToLower()];
                }
                Debug.LogError("[" + (name + " (networkinstantiateconfig)").ToLower() + "|null] and  [" + (name + "").ToLower() + "|null]");
                return null;
            }
            static Dictionary<string, NetworkInstantiateConfig> KeysData = new Dictionary<string, NetworkInstantiateConfig>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<NetworkInstantiateConfig>())
                {
                    if (st != null)
                    {
                        if (!(st + "").ToLower().Contains("(clone)"))
                        {
                            if (!KeysData.ContainsKey((st + "").ToLower()))
                            {
                                KeysData.Add((st + "").ToLower(), st);
                            }
                        }
                    }
                }
            }
        }
        struct Objects
        {
            public static GameObject GetKey(string name)
            {
                if (KeysData.Count() < 5) LoadKey();
                if (KeysData.ContainsKey((name + "(gameobject)").ToLower()))
                {
                    return KeysData[(name + "(gameobject)").ToLower()];
                }
                if (KeysData.ContainsKey((name + "").ToLower()))
                {
                    return KeysData[(name + "").ToLower()];
                }
                /*foreach (var key in KeysData)
				{
					Warning("[Name: " + key.Key + "|GameObject: " + key.Value + "]");
				}*/
                Debug.LogError("[" + (name + "(gameobject)").ToLower() + "|null] and  [" + (name + "").ToLower() + "|null]");
                return null;
            }
            static Dictionary<string, GameObject> KeysData = new Dictionary<string, GameObject>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (st != null)
                    {
                        if (!KeysData.ContainsKey((st.name + "").ToLower()))
                        {
                            //Warning("[Name: " + key.Key + "|GameObject: " + key.Value + "]");
                            if (!(st.name + "").ToLower().Contains("(clone)"))
                            {
                                KeysData.Add((st.name + "").ToLower(), st);
                            }
                        }
                    }
                }
            }
        }
        struct ItemInstance
        {
            public static ItemGeneratorAsset GetKey(int id)
            {
                if (KeysData.Count() < 5) LoadKey();
                if (KeysData.ContainsKey(id))
                    return KeysData[id];
                foreach (var st in Singleton<GlobalItemManager>.Instance.GetGenerators())
                {
                    if (st.Value != null)
                    {
                        if (st.Key == id)
                        {
                            return st.Value;
                        }
                    }
                }
                return null;
            }
            public static bool Equals(ItemGeneratorAsset Value1, ItemGeneratorAsset Value2)
            {
                return ((Value1 == Value2) || (Value1.GeneratorId == Value2.GeneratorId));
            }
            public static int GetValue(ItemGeneratorAsset Value)
            {
                if (Value != null)
                    if (ValuesData.ContainsKey(Value))
                        return ValuesData[Value];
                return 0;
            }
            static Dictionary<int, ItemGeneratorAsset> KeysData = new Dictionary<int, ItemGeneratorAsset>();
            static Dictionary<ItemGeneratorAsset, int> ValuesData = new Dictionary<ItemGeneratorAsset, int>();
            public static void LoadKey(int i = 0)
            {
                foreach (var st in Singleton<GlobalItemManager>.Instance.GetGenerators())
                {
                    if (st.Value != null)
                    {
                        if (!KeysData.ContainsKey(st.Key))
                        {
                            KeysData.Add(st.Key, st.Value);
                        }
                        if (!ValuesData.ContainsKey(st.Value))
                        {
                            ValuesData.Add(st.Value, st.Key);
                        }
                    }
                }
            }
        }
    }
}