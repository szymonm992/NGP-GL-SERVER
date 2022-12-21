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
    }
}
