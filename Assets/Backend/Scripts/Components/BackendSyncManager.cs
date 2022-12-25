using Backend.Scripts.Models;
using Backend.Scripts.Signals;
using Frontend.Scripts;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.Networking.Components;
using GLShared.Networking.Extensions;
using GLShared.Networking.Interfaces;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class BackendSyncManager : MonoBehaviour, ISyncManager
    {
        [Inject] private readonly IVehiclesDatabase vehicleDatabase;
        [Inject] private readonly PlayerSpawner playerSpawner;
        [Inject] private readonly SignalBus signalBus;
        [Inject] private readonly RoomManager roomManager;
        [Inject] private readonly SmartFoxConnection smartFox;

        private readonly Dictionary<string, INetworkEntity> connectedPlayers = new Dictionary<string, INetworkEntity>();

        private int spawnedPlayersAmount = 0;

        public int SpawnedPlayersAmount => spawnedPlayersAmount;
        public double CurrentServerTime => 0;

        public void Initialize()
        {
        }

        public void TryCreatePlayer(User user, Vector3 spawnPosition, Vector3 spawnEulerAngles)
        {
            if(!connectedPlayers.ContainsKey(user.Name))
            {
                CreatePlayer(user, spawnPosition, spawnEulerAngles);
            }
        }

        public void SyncPosition(INetworkEntity entity)
        {
            if(entity.IsPlayer)
            {
                ISFSObject data = entity.CurrentNetworkTransform.ToISFSOBject();
                ExtensionRequest request = new ExtensionRequest("inbattle.playerSync", data, null, false);
                smartFox.Connection.Send(request);
            }
        }

        private void CreatePlayer(User user, Vector3 spawnPosition, Vector3 spawnEulerAngles)
        {
            var vehicleName = user.GetVariable("playerVehicle").Value.ToString();
            var playerProperties = GetPlayerInitData(user, vehicleName, spawnPosition, spawnEulerAngles);
            var prefabEntity = playerProperties.PlayerContext.gameObject.GetComponent<PlayerEntity>();//this references only to prefab
            var playerEntity = playerSpawner.Spawn(prefabEntity, playerProperties);

            signalBus.Fire(new SyncSignals.OnPlayerSpawned()
            {
                PlayerProperties = playerProperties,
            });    

            connectedPlayers.Add(user.Name, playerEntity);
            spawnedPlayersAmount++;
        }

        private PlayerProperties GetPlayerInitData(User user, string vehicleName, 
            Vector3 spawnPosition, Vector3 spawnEulerAngles)
        {
            //TODO: handling check whether the player is local or not

            var vehicleData = vehicleDatabase.GetVehicleInfo(vehicleName);
            if (vehicleData != null)
            {
                return new PlayerProperties()
                {
                    PlayerContext = vehicleData.VehiclePrefab,
                    PlayerVehicleName = vehicleData.VehicleName,
                    IsLocal = false,
                    SpawnPosition = spawnPosition,
                    SpawnRotation = Quaternion.Euler(spawnEulerAngles.x, spawnEulerAngles.y, spawnEulerAngles.z),
                    User = user,
                };
            }
            return null;
        }
    }
}

