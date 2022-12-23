using Backend.Scripts.Models;
using Backend.Scripts.Signals;
using Frontend.Scripts;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.Networking.Components;
using GLShared.Networking.Interfaces;
using Sfs2X.Entities;
using System.Collections;
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

        private int spanwedPlayersAmount = 0;

        public int SpawnedPlayersAmount => spanwedPlayersAmount;
        public double CurrentServerTime => 0;

        public void Initialize()
        {
        }

        public void TryCreatePlayer(User user, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if(!connectedPlayers.ContainsKey(user.Name))
            {
                CreatePlayer(user, spawnPosition, spawnRotation);
            }
        }

        private void CreatePlayer(User user, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var vehicleName = user.GetVariable("playerVehicle").Value.ToString();
            var playerProperties = GetPlayerInitData(user, vehicleName, spawnPosition, spawnRotation);
            var prefabEntity = playerProperties.PlayerContext.gameObject.GetComponent<PlayerEntity>();//this references only to prefab
            var playerEntity = playerSpawner.Spawn(prefabEntity, playerProperties);

            signalBus.Fire(new SyncSignals.OnPlayerSpawned()
            {
                PlayerProperties = playerProperties,
            });    

            connectedPlayers.Add(user.Name, playerEntity);
            spanwedPlayersAmount++;
        }

        private PlayerProperties GetPlayerInitData(User user, string vehicleName, 
            Vector3 spawnPosition, Quaternion spawnRotation)
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
                    SpawnRotation = spawnRotation,
                    User = user,
                };
            }
            return null;
        }
    }
}

