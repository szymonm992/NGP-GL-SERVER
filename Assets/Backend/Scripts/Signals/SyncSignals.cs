using GLShared.General.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Scripts.Signals
{
    public class SyncSignals
    {
        public class OnPlayerSpawned
        {
            public PlayerProperties PlayerProperties { get; set; }
        }

        public class OnGameStateChanged
        {
            public int CurrentGameStateIndex { get; set; }
        }

        public class OnGameCountdownUpdate
        {
            public int CurrentCountdownValue { get; set; }
        }
    }
}
