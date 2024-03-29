using Backend.Scripts.Signals;
using GLShared.General.Enums;
using GLShared.General.Models;
using GLShared.General.Signals;
using GLShared.Networking.Components;
using GLShared.Networking.Extensions;
using GLShared.Networking.Models;
using Sfs2X.Requests;
using UnityEngine;

namespace Backend.Scripts.Components
{
    public class BackendSyncManager : SyncManagerBase
    {
        public override void Initialize()
        {
            base.Initialize();

            signalBus.Subscribe<PlayerSignals.OnPlayerShot>(OnPlayerShot);
        }

        public override void SyncPosition(PlayerEntity entity)
        {
            base.SyncPosition(entity);

            var room = smartFox.Connection.LastJoinedRoom;
            var data = entity.CurrentTransform.ToISFSOBject();
            var request = new ExtensionRequest(NetworkConsts.RPC_PLAYER_SYNC, data, room, false);

            smartFox.Connection.Send(request);
        }

        public override void SyncInputs(PlayerInput input)
        {
            if (connectedPlayers.ContainsKey(input.Username))
            {
                base.SyncInputs(input);

                connectedPlayers[input.Username].InputProvider.SetInput(input);
            }
        }

        public override void SyncShell(ShellEntity shellEntity)
        {
            base.SyncShell(shellEntity);

            var room = smartFox.Connection.LastJoinedRoom;
            var data = shellEntity.CurrentTransform.ToISFSOBject();
            var request = new ExtensionRequest(NetworkConsts.RPC_SHELL_SYNC, data, room, false);

            smartFox.Connection.Send(request);
        }

        protected override void CreateShell(string username, string databaseIdentifier, int sceneIdentifier, Vector3 spawnPosition, Vector3 spawnEulerAngles,
            (Vector3, float) targetingProperties, out ShellProperties shellProperties)
        {
            base.CreateShell(username, databaseIdentifier, sceneIdentifier, spawnPosition, spawnEulerAngles, targetingProperties, out shellProperties);

            signalBus.Fire(new ShellSignals.OnShellSpawned()
            {
                ShellProperties = shellProperties,
            });
        }

        protected override void CreatePlayer(string username, Team team, Vector3 spawnPosition, Vector3 spawnEulerAngles, out PlayerProperties playerProperties)
        {
            base.CreatePlayer(username, team, spawnPosition, spawnEulerAngles, out playerProperties);
            
            signalBus.Fire(new SyncSignals.OnPlayerSpawned()
            {
                PlayerProperties = playerProperties,
            });
        }

        protected override PlayerProperties GetPlayerInitData(string username, Team team, string vehicleName, 
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
                    Username = username,
                    Team = team.ToString(),
                };
            }

            return null;
        }

        private void OnPlayerShot(PlayerSignals.OnPlayerShot OnPlayerShot)
        {
            TryCreateShell(OnPlayerShot.Username, OnPlayerShot.ShellId, spawnedShellsAmount, OnPlayerShot.ShellSpawnPosition, OnPlayerShot.ShellSpawnEulerAngles, OnPlayerShot.TargetingProperties);
        }
    }
}

