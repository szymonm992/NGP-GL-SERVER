using GLShared.General.Models;
using GLShared.General.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Backend.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ShellsDatabase", menuName = "UT/Databases/Backend shells database")]
    public class BackendShellsDatabase : ShellsDatabase
    {
        [SerializeField] private ShellEntryInfo[] allShells;

        public override IEnumerable<ShellEntryInfo> AllShells => allShells;
    }
}
