using Backend.Scripts.Components;
using GLShared.General.Models;
using GLShared.General.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BackendVehiclesDatabase", menuName = "UT/Databases/BackendVehiclesDatabase")]
    public class BackendVehiclesDatabase : VehiclesDatabase
    {
        [SerializeField] private VehicleEntryInfo[] allVehicles;

        public override IEnumerable<VehicleEntryInfo> AllVehicles => allVehicles;
    }
}
