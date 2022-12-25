using Backend.Scripts.Models;
using Backend.Scripts.Signals;
using GLShared.General.Interfaces;
using GLShared.General.Models;
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
        [Inject] private readonly RoomManager roomManager;

        public override void SyncPosition(INetworkEntity entity)
        {
            if(entity.IsPlayer)
            {
                ISFSObject data = entity.CurrentNetworkTransform.ToISFSOBject();
                ExtensionRequest request = new ExtensionRequest("inbattle.playerSync", data, null, false);
                smartFox.Connection.Send(request);
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

