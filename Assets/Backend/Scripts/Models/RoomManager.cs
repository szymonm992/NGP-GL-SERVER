using Backend.Scripts.Signals;
using GLShared.Networking.Components;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Models
{
    public class RoomManager : IInitializable, ITickable
    {
        [Inject] private readonly SignalBus signalBus;
        [Inject] private readonly SmartFoxConnection connection;

        private Room currentRoom;

        private const string ADMIN_USER_PWD = "777 777 777";
        private const string ZONE_NAME = "GLServerGateway";

        public void Initialize()
        {
            signalBus.Subscribe<SyncSignals.OnGameStateChanged>(OnGameStateChanged);
            signalBus.Subscribe<SyncSignals.OnGameCountdownUpdate>(OnGameCountdownUpdate);
            ConnectToServerGateway();
        }

        private void OnGameStateChanged(SyncSignals.OnGameStateChanged OnGameStateChanged)
        {
            ISFSObject data = new SFSObject();
            data.PutInt("currentGameStage", OnGameStateChanged.CurrentGameStateIndex);
            ExtensionRequest request = new ExtensionRequest("inbattle.gameStage", data, null, false);
            connection.Connection.Send(request);
        }

        private void ConnectToServerGateway()
        {
            connection.Connection = new SmartFox()
            {
                ThreadSafeMode = true,
            };

            connection.Connection.AddEventListener(SFSEvent.CONNECTION, OnConnection);
            connection.Connection.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            connection.Connection.AddEventListener(SFSEvent.LOGIN, OnLogin);
            connection.Connection.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            connection.Connection.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
            connection.Connection.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
            connection.Connection.AddEventListener(SFSEvent.UDP_INIT, OnUDPInit);
            connection.Connection.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnExitRoom);

            ConfigData serverConfig = new ConfigData();
            serverConfig.Host = "127.0.0.1";
            serverConfig.Port = 9933;
            serverConfig.Zone = ZONE_NAME;
            serverConfig.UdpHost = "127.0.0.1";
            serverConfig.UdpPort = 9933;

            connection.Connection.Connect(serverConfig);
        }

        private void OnGameCountdownUpdate(SyncSignals.OnGameCountdownUpdate OnGameCountdownUpdate)
        {
            ISFSObject data = new SFSObject();
            data.PutInt("currentCountdownValue", OnGameCountdownUpdate.CurrentCountdownValue);
            ExtensionRequest request = new ExtensionRequest("inbattle.gameStartCountdown", data, null, false);
            connection.Connection.Send(request);
        }

        private void OnExtensionResponse(BaseEvent evt)
        {
            try
            {
                string cmd = (string)evt.Params["cmd"];
                ISFSObject objIn = (SFSObject)evt.Params["params"];

                if (cmd == "inputs")
                {

                }
            }
            catch (System.Exception exception)
            {
                Debug.Log("[SERVERAPP DEBUG] Exception handling response: " + exception.Message
                    + " >>>[AND TRACE IS]>>> " + exception.StackTrace);
            }
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
                Debug.Log("[SERVERAPP DEBUG] UDP initialized successfully: " + connection.Connection.UdpAvailable
                    + "|" + connection.Connection.UdpInited);
            }
            else
            {
                Debug.Log("[SERVERAPP DEBUG] Error with udp unit");
            }
        }

        private void OnLogin(BaseEvent evt)
        {
            ISFSObject data = new SFSObject();
            SendRoomJoinRequest("adminJoinRoom", data);
            connection.Connection.InitUDP();
        }

        private void SendRoomJoinRequest(string cmd, ISFSObject data)
        {
            Room room = connection.Connection.LastJoinedRoom;
            ExtensionRequest request = new ExtensionRequest(cmd, data, room, false);
            connection.Connection.Send(request);
        }

        private void OnRoomJoin(BaseEvent evt)
        {
            currentRoom = connection.Connection.LastJoinedRoom;
        }

        #region ERROR/DISCONNECT
        private void OnExitRoom(BaseEvent evt)
        {
            SFSUser user = (SFSUser)evt.Params["user"];
            // left_users.Add(user.Name);
            //players.Remove(user);
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
        #endregion

        private void TryLogin()
        {
            string username = "$ADMIN$(" + System.DateTime.UtcNow.Millisecond.ToString() 
                + "|" + (Random.Range(1, 99999).ToString() + ")").ToString();

            connection.Connection.Send(new LoginRequest(username, ADMIN_USER_PWD, ZONE_NAME));
        }

        public void Tick()
        {
            if (connection.IsInitialized)
            {
                connection.Connection.ProcessEvents();
            }
        }
    }
}
