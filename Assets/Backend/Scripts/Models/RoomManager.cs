using Backend.Scripts.Components;
using Backend.Scripts.Signals;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using GLShared.General.Signals;
using GLShared.Networking.Components;
using GLShared.Networking.Extensions;
using GLShared.Networking.Models;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Models
{
    public class RoomManager : IInitializable, ITickable
    {
        private const string ADMIN_USER_PWD = "777 777 777";
        private const string ZONE_NAME = "GLServerGateway";
        private const string LOCALHOST_ADRESS = "127.0.0.1";
        private const int CONNECTION_PORT = 9933;

        [Inject] private readonly SignalBus signalBus;
        [Inject] private readonly SmartFoxConnection smartFox;
        [Inject] private readonly IBattleManager battleManager;
        [Inject] private readonly ISyncManager syncManager;
        [Inject] private readonly MapManager mapManager;

        private Room currentRoom;

        public void Initialize()
        {
            signalBus.Subscribe<SyncSignals.OnGameStateChanged>(OnGameStateChanged);
            signalBus.Subscribe<SyncSignals.OnGameCountdownUpdate>(OnGameCountdownUpdate);
            signalBus.Subscribe<SyncSignals.OnPlayerSpawned>(OnPlayerSpawned);
            signalBus.Subscribe<PlayerSignals.OnBattleTimeChanged>(OnBattleTimeChanged);
            signalBus.Subscribe<PlayerSignals.OnPlayerShot>(OnPlayerShot);

            ConnectToServerGateway();
        }

        private void OnGameStateChanged(SyncSignals.OnGameStateChanged OnGameStateChanged)
        {
            SendCurrentGameState(OnGameStateChanged.CurrentGameStateIndex);
        }

        private void ConnectToServerGateway()
        {
            smartFox.Connection = new SmartFox()
            {
                ThreadSafeMode = true,
            };

            smartFox.Connection.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            smartFox.Connection.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            smartFox.Connection.AddEventListener(SFSEvent.LOGIN, OnLogin);
            smartFox.Connection.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            smartFox.Connection.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
            smartFox.Connection.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
            smartFox.Connection.AddEventListener(SFSEvent.UDP_INIT, OnUDPInit);
            smartFox.Connection.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
            smartFox.Connection.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
            smartFox.Connection.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);

            ConfigData connectionConfigData = new()
            {
                Host = LOCALHOST_ADRESS,
                Port = CONNECTION_PORT,
                Zone = ZONE_NAME,
                UdpHost = LOCALHOST_ADRESS,
                UdpPort = CONNECTION_PORT,
            };

            smartFox.Connection.Connect(connectionConfigData);
        }

        private void OnGameCountdownUpdate(SyncSignals.OnGameCountdownUpdate OnGameCountdownUpdate)
        {
            var room = smartFox.Connection.LastJoinedRoom;

            ISFSObject data = new SFSObject();
            data.PutInt(NetworkConsts.VAR_CURRENT_COUNTDOWN, OnGameCountdownUpdate.CurrentCountdownValue);
            var request = new ExtensionRequest(NetworkConsts.RPC_GAME_START_COUNTDOWN, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void OnPlayerSpawned(SyncSignals.OnPlayerSpawned OnPlayerSpawned)
        {
            var room = smartFox.Connection.LastJoinedRoom;

            ISFSObject data = OnPlayerSpawned.PlayerProperties.ToISFSOBject();
            var request = new ExtensionRequest(NetworkConsts.RPC_PLAYER_SPAWNED, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void OnPlayerShot(PlayerSignals.OnPlayerShot OnPlayerShot)
        {
            var room = smartFox.Connection.LastJoinedRoom;

            ISFSObject data = null;
            var request = new ExtensionRequest(NetworkConsts.RPC_PLAYER_SHOT, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void OnBattleTimeChanged(PlayerSignals.OnBattleTimeChanged OnBattleTimeChanged)
        {
            var room = smartFox.Connection.LastJoinedRoom;
            var data = new SFSObject();

            data.PutInt(NetworkConsts.VAR_MINUTES_LEFT, OnBattleTimeChanged.CurrentMinutesLeft);
            data.PutInt(NetworkConsts.VAR_SECONDS_LEFT, OnBattleTimeChanged.CurrentSecondsLeft);
            var request = new ExtensionRequest(NetworkConsts.RPC_BATTLE_TIMER, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void OnExtensionResponse(BaseEvent evt)
        {
            try
            {
                string cmd = (string)evt.Params["cmd"];
                ISFSObject objIn = (SFSObject)evt.Params["params"];

                if (cmd == "playerInputs")
                {
                    PlayerInput input = objIn.ToPlayerInput();
                    syncManager.SyncInputs(input);
                }
            }
            catch (System.Exception exception)
            {
                Debug.Log("[SERVERAPP DEBUG] Exception handling response: " + exception.Message
                    + " >>>[AND TRACE IS]>>> " + exception.StackTrace);
            }
        }

        private void SendCurrentGameState(int gameStateIndex)
        {
            var room = smartFox.Connection.LastJoinedRoom;

            ISFSObject data = new SFSObject();
            data.PutInt(NetworkConsts.VAR_CURRENT_GAME_STAGE, gameStateIndex);
            var request = new ExtensionRequest(NetworkConsts.RPC_SEND_GAME_STATE, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void SendRoomJoinRequest(string cmd, ISFSObject data)
        {
            Room room = smartFox.Connection.LastJoinedRoom;

            var request = new ExtensionRequest(cmd, data, room, false);

            smartFox.Connection.Send(request);
        }

        private void OnRoomJoin(BaseEvent evt)
        {
            currentRoom = smartFox.Connection.LastJoinedRoom;
            var userList = currentRoom.UserList.Where(u => !u.IsAdmin());

            if (userList.Any())
            {
                foreach (var actualUser in userList)
                {
                    Debug.Log("[SERVERAPP DEBUG] Creating player " + actualUser.Name + "| IsAdmin:" + actualUser.IsAdmin());

                    if (actualUser.ContainsVariable("team"))
                    {
                        Team team = (Team)System.Convert.ToInt32(actualUser.GetVariable("team").Value);
                        var freeSpawnPoint = mapManager.GetFreeSpawnPoint(team);

                        if (freeSpawnPoint != null)
                        {
                            syncManager.TryCreatePlayer(actualUser, freeSpawnPoint.SpawnPosition, freeSpawnPoint.SpawnEulerAngles);
                            freeSpawnPoint.SetFree(false);
                        }
                        else
                        {
                            Debug.LogError("No empty spawn point has been found for team " + team.ToString());
                        }
                    }
                    else
                    {
                        Debug.LogError("Player does not contain a team variable!");
                    }
                }
            }
            else
            {
                Debug.Log("Did not find any player!");
            }
        }

        #region ERROR/DISCONNECT
        private void OnUserExitRoom(BaseEvent evt)
        {
            SFSUser user = (SFSUser)evt.Params["user"];
        }
        
        private void OnUserEnterRoom(BaseEvent evt)
        {
            SFSUser user = (SFSUser)evt.Params["user"];

            if (user == smartFox.Connection.MySelf)
            {
                if (battleManager.CurrentBattleStage != BattleStage.Beginning)
                {
                    SendCurrentGameState((int)battleManager.CurrentBattleStage);
                }
            }
        }

        private void OnConnectionLost(BaseEvent evt)
        {
            string reason = (string)evt.Params["reason"];

            if (reason != ClientDisconnectionReason.MANUAL)
            {
                Debug.Log("[SERVERAPP DEBUG] Connection was lost; reason is: " + reason);
            }

            Application.Quit();
        }

        private void OnLoginError(BaseEvent evt)
        {
            Debug.Log("[SERVERAPP DEBUG] Login failed: " + (string)evt.Params["errorMessage"]);
        }

        private void OnRoomJoinError(BaseEvent evt)
        {
            Debug.Log("[SERVERAPP DEBUG] Room join failed: " + (string)evt.Params["errorMessage"]);
        }

        private void OnConnection(BaseEvent evt)
        {
            if ((bool)evt.Params["success"])
            {
                TryLogin();
            }
            else
            {
                Debug.Log("[SERVERAPP DEBUG] Failed connecting to server application!");
            }
        }

        private void OnUDPInit(BaseEvent evt)
        {
            if ((bool)evt.Params["success"])
            {
                Debug.Log("[SERVERAPP DEBUG] UDP initialized successfully: " + smartFox.Connection.UdpAvailable
                    + "|" + smartFox.Connection.UdpInited);
            }
            else
            {
                Debug.Log("[SERVERAPP DEBUG] Error with udp unit");
            }
        }

        private void OnLogin(BaseEvent evt)
        {
            var data = new SFSObject();
            SendRoomJoinRequest(NetworkConsts.REQ_ADMIN_JOIN_ROOM, data);
            smartFox.Connection.InitUDP();
        }
        #endregion

        private void TryLogin()
        {
            string username = $"$ADMIN$({System.DateTime.UtcNow.Millisecond} | {Random.Range(1, 99999)})";
            smartFox.Connection.Send(new LoginRequest(username, ADMIN_USER_PWD, ZONE_NAME));
        }

        public void Tick()
        {
            if (smartFox.IsInitialized)
            {
                smartFox.Connection.ProcessEvents();
            }
        }
    }
}
