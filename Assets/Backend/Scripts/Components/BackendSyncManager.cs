using GLShared.General.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Scripts.Components
{
    public class BackendSyncManager : MonoBehaviour, ISyncManager
    {
        private int spawnedPlayersAmount = 0;
        public int SpawnedPlayersAmount => throw new System.NotImplementedException();

        public void CreatePlayer(bool isLocal, string vehicleName, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            
        }
    }
}

