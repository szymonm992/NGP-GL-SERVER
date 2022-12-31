using GLShared.General.Interfaces;
using GLShared.General.Signals;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Models
{
    public class DetectionManager : IInitializable, ITickable
    {
        [Inject] private readonly SignalBus signalBus;
        [Inject] private readonly ISyncManager syncManager;

        public void Initialize()
        {
            signalBus.Subscribe<PlayerSignals.OnPlayerDetectionStatusUpdate>(OnPlayerDetectionStatusUpdate);
        }

        public void Tick()
        {
            if(syncManager.SpawnedPlayersAmount > 0)
            {
                HandlePlayersDetecting();
            }
        }

        private void HandlePlayersDetecting()
        {

        }

        private void OnPlayerDetectionStatusUpdate(PlayerSignals.OnPlayerDetectionStatusUpdate OnPlayerSpottedStatusUpdate)
        {

        }
    }
}
