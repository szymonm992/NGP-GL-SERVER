using Backend.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;
using GLShared.General.Enums;

namespace Backend.Scripts.Components
{
    public class MapManager : MonoBehaviour, IInitializable
    {
        [SerializeField] private SpawnPoint[] spawnPoints;

        public IEnumerable<SpawnPoint> SpawnPoints => spawnPoints;

        public void Initialize()
        {
            if(!spawnPoints.Any())
            {
                Debug.LogError("Them ap does not have any spawn points set!");
            }
        }

        public SpawnPoint GetFreeSpawnPoint(Team team)
        {
            var sortedPoints = spawnPoints.Where(point => point.SpawnPointTeam == team && point.Isfree).ToArray();
            if (sortedPoints.Any())
            {
                int index = Random.Range(0, sortedPoints.Length);
                return sortedPoints[index];
            }
            return null;
        }

    }
}
