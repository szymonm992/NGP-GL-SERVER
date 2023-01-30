using Backend.Scripts.Models;
using Backend.Scripts.Signals;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.Signals;
using GLShared.Networking.Components;
using GLShared.Networking.Extensions;
using GLShared.Networking.Interfaces;
using GLShared.Networking.Models;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class BackendSyncManager : SyncManagerBase
    {
        public override void Initialize()
        {
            base.Initialize();
            signalBus.Subscribe<PlayerSignals.OnPlayerShot>(OnPlayerShot);
        }

        public override void SyncPosition(INetworkEntity entity)
        {
            if (entity.IsPlayer)
            {
                base.SyncPosition(entity);
                var room = smartFox.Connection.LastJoinedRoom;
                var data = entity.CurrentNetworkTransform.ToISFSOBject();
                var request = new ExtensionRequest(NetworkConsts.RPC_PLAYER_SYNC, data, room, false);
                smartFox.Connection.Send(request);
            }
        }

        public override void SyncInputs(PlayerInput input)
        {
            if(connectedPlayers.ContainsKey(input.Username))
            {
                base.SyncInputs(input);
                connectedPlayers[input.Username].InputProvider.SetInput(input);
            }
        }

        public override void SyncShell(IShellController shellController)
        {
            if (connectedPlayers.ContainsKey(shellController.OwnerUsername))
            {
                base.SyncShell(shellController);
            }
        }

        protected override void CreatePlayer(User user, Vector3 spawnPosition, Vector3 spawnEulerAngles, out PlayerProperties playerProperties)
        {
            base.CreatePlayer(user, spawnPosition, spawnEulerAngles, out playerProperties);
            
            signalBus.Fire(new SyncSignals.OnPlayerSpawned()
            {
                PlayerProperties = playerProperties,
            });
        }

        protected override PlayerProperties GetPlayerInitData(User user, string vehicleName, 
            Vector3 spawnPosition, Vector3 spawnEulerAngles)
        {
            var vehicleData = vehicleDatabase.GetVehicleInfo(vehicleName);

            if (vehicleData != null)
            {
                return new ()
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

        private void OnPlayerShot(PlayerSignals.OnPlayerShot OnPlayerShot)
        {
            TryCreateShell(OnPlayerShot.Username, OnPlayerShot.ShellId);
        }
    }
}

