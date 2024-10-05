using Client;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using YuchiGames.POM.Shared;
using YuchiGames.POM.Shared.DataObjects;
using YuchiGames.POM.Shared.Utils;
using static Il2Cpp.CubeGenerator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace YuchiGames.POM.Client.Managers
{
    
    public class GroupSyncerComponent : MonoBehaviour
    {
        public GroupSyncerComponent(IntPtr ptr) : base(ptr) { }

        public int HostID = Network.ID;
        public ObjectUID GroupUID = Network.NextObjectUID();
        public int TakeID = 0;

        public RigidbodyManager RBM => _rbm ?? (_rbm = GetComponent<RigidbodyManager>());
        private RigidbodyManager? _rbm = null;

        bool _unloaded = false;

        public void Unload()
        {
            _unloaded = true;
            SendUpdate();
            Network.Send(new GroupSetHostMessage() { GroupID = GroupUID, NewHostID = -1 });
        }

        public void Start()
        {
            if (Network.ID < 0)
            {
                GameObject.Destroy(this);
                return;
            }

            Network.SyncedObjects[GroupUID] = this;

            gameObject.name += $"[SYNCED {GroupUID}]";

            if (Network.IsConnected)
                InserAwaitForUpdateObjectSorted(this);
        }

        public void OnDestroy()
        {
            if (Network.ID < 0)
                return;

            if (!_unloaded && IsHosted)
                Network.Send(new GroupDestroyedMessage { GroupID = GroupUID } );
            Network.SyncedObjects.Remove(GroupUID);
        }

        public void OnCollisionStay(UnityEngine.Collision collision)
        {
            if (!IsHosted)
                return;
            if (collision.gameObject.TryGetComponent<GroupSyncerComponent>(out var syncedObject))
                TryToClaimHost(syncedObject);
        }
        public void TryToClaimHost(GroupSyncerComponent other)
        {
            if (ShouldTake(other))
                Network.ClaimHost(other.GroupUID);
        }

        public bool IsHosted => Network.ID == HostID;
        /// <summary>
        /// Do that object have priority above <paramref name="other"/> object
        /// </summary>
        /// <returns>true, if it have priority, otherwise false</returns>
        public bool ShouldTake(GroupSyncerComponent other)
        {
            if (HostID == other.HostID)
                return false;
            if (TakeID == other.TakeID)
                return other.GroupUID < GroupUID;
            return TakeID > other.TakeID;
        }

        
        public void SendQuickUpdate()
        {
            Network.Send(Network.GetQuickUpdateData(RBM, GroupUID));
        }
        public void SendUpdate()
        {
            Network.Send(new GroupUpdateMessage()
            {
                GroupData = Network.GetUpdateData(RBM),
                GroupUID = GroupUID,
            });
        }


        #region Sync tasks
        class SyncerTask
        {
            public float TimePasses = 0f;
            public float TimeBetweenUpdates;
            public Action<IEnumerable<GroupSyncerComponent>> UpdateAction = delegate { };
        }

        public static float WorldUpdateDistance { get; set; } // Synced with server
        public static float WorldQuickUpdateDistance { get; set; } // Synced with server

        private static LinkedList<(GroupSyncerComponent component, float distance)> s_awaitsForUpdateObjects = new();

        static float TakeMinDistance(Vector3 pos) =>
            Player.ConnectedPlayers.Any() ? Player.ConnectedPlayers
                .Select(pair => Vector3.Distance(pos, pair.Value.PlayerLastPositionData.ToUnity()))
                .Min() : 0;

        static void InserAwaitForUpdateObjectSorted(GroupSyncerComponent component)
        {
            if (!s_awaitsForUpdateObjects.Any())
            {
                s_awaitsForUpdateObjects.AddLast((component, TakeMinDistance(component.gameObject.transform.position)));
                return;
            }

            var dist = TakeMinDistance(component.transform.position);

            var current = s_awaitsForUpdateObjects.First;
            while (current != null && current.Value.distance < dist)
                current = current.Next;

            if (current == null)
                s_awaitsForUpdateObjects.AddLast((component, dist));
            else
                s_awaitsForUpdateObjects.AddBefore(current, (component, dist));
        }

        private static List<SyncerTask> s_tasks = new()
        {
            new() // Resort everything once in a second
            {
                TimeBetweenUpdates = 1f,
                UpdateAction = groups =>
                {
                    var tlist = s_awaitsForUpdateObjects.ToList();
                    tlist.Sort((a, b) => a.Item2 < b.Item2 ? -1 : 1);
                    s_awaitsForUpdateObjects = new(tlist);
                }
            },
            new() // Full object data update (updates 1 object per invoke)
            {
                TimeBetweenUpdates = 1f/100f,
                UpdateAction = groups =>
                {
                    if (!s_awaitsForUpdateObjects.Any())
                    {
                        Log.Information("No forced sync required. Collecting all groups...");

                        var tlist = groups.Select(obj => (obj, TakeMinDistance(obj.transform.position))).ToList();
                        tlist.Sort((a, b) => a.Item2 < b.Item2 ? -1 : 1);
                        s_awaitsForUpdateObjects = new(tlist);

                        Log.Information($"Collected {tlist.Count} groups");
                    }

                    if (!s_awaitsForUpdateObjects.Any()) return;

                    var nodeForUpdate = s_awaitsForUpdateObjects.First;

                    while (nodeForUpdate != null && nodeForUpdate.Value.component == null)
                        nodeForUpdate = nodeForUpdate.Next;

                    if (nodeForUpdate == null)
                        return;

                    var elementForUpdate = nodeForUpdate.Value.component;

                    elementForUpdate.SendUpdate();

                    s_awaitsForUpdateObjects.RemoveFirst();
                }
            },
            new() // Quick update (position + rotation + velocity + angular velocity) (updates all objects per invoke)
            {
                TimeBetweenUpdates = 0,
                UpdateAction = groups =>
                {
                    var sorted = groups
                        .Select(obj => (obj, Player.ConnectedPlayers
                            .Select(pair => Vector3.Distance(obj.gameObject.transform.position, pair.Value.PlayerLastPositionData.ToUnity()))
                            .Min()))
                        .Where(obj => obj.Item2 < WorldQuickUpdateDistance)
                        .Select(obj => obj.obj);

                    foreach (var e in sorted)
                        e.SendQuickUpdate();

                    return;
                }
            }
        };

        public static void GlobalUpdate()
        {
            if (!Network.IsConnected) return;

            if (Player.ConnectedPlayers.Count <= 0) return;

            foreach (var task in s_tasks)
            {
                task.TimePasses += Time.deltaTime;

                if (task.TimePasses > task.TimeBetweenUpdates)
                {
                    if (task.TimeBetweenUpdates > 0)
                        task.TimePasses %= task.TimeBetweenUpdates;

                    var groups = GameObject.FindObjectsOfTypeAll(Il2CppType.Of<GroupSyncerComponent>())
                        .Select(obj => obj.Cast<GroupSyncerComponent>())
                        .Where(obj => obj.HostID == Network.ID);

                    task.UpdateAction(groups);
                }
            }
        }
        #endregion
    }
}
