using UnityEngine;

namespace Backend.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ShellsSettings", menuName = "UT/Settings/Shells settings")]
    public class ShellsSettings : ScriptableObject
    {
        [SerializeField] private LayerMask obstaclesAndArmorMask;
        [SerializeField] private LayerMask armorLayerMask;
        [SerializeField] private float autopenCaliberDifference = 3f;

        public LayerMask ObstaclesAndArmorMask => obstaclesAndArmorMask;
        public LayerMask ArmorLayerMask => armorLayerMask;
        public float AutopenCaliberDifference => autopenCaliberDifference;
    }
}
