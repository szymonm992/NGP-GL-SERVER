using GLShared.General.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Scripts.Models
{
    [System.Serializable]
    public class SpawnPoint 
    {
        [SerializeField] private Transform spawnPointTransform;
        [SerializeField] private Team spawnPointTeam;

        private bool isFree = true;

        public Team SpawnPointTeam => spawnPointTeam;
        public bool Isfree => isFree;
        public Vector3 SpawnPosition => spawnPointTransform.position;
        public Vector3 SpawnEulerAngles => spawnPointTransform.eulerAngles;
        public Quaternion SpawnRotation => Quaternion.Euler(spawnPointTransform.eulerAngles);
        
        public void SetFree(bool value)
        {
            this.isFree = value;
        }
    }
}
