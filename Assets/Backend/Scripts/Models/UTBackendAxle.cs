using GLShared.General.Components;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using GLShared.General.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Models
{
    public class UTBackendAxle : UTAxleBase
    {
        [SerializeField] protected UTAxlePairBase[] wheelPairs;

        public override IEnumerable<UTAxlePairBase> WheelPairs => wheelPairs;

        public override IEnumerable<IPhysicsWheel> GetAllWheelsOfAxis(DriveAxisSite axis)
        {
            return wheelPairs.Where(pair => pair.Axis == axis).Select(pair => pair.Wheel).ToArray();
        }

        protected void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            bool drawCurrently = (debugSettings.DrawGizmos) && (debugSettings.DrawMode == UTDebugMode.All)
               || (debugSettings.DrawMode == UTDebugMode.EditorOnly && !Application.isPlaying)
               || (debugSettings.DrawMode == UTDebugMode.PlaymodeOnly && Application.isPlaying);

            if (drawCurrently)
            {
                Gizmos.color = Color.white;
                if (debugSettings.DrawAxleCenter)
                {
                    Gizmos.DrawSphere(transform.position, .11f);
                }

                if (debugSettings.DrawAxlePipes)
                {
                    if (wheelPairs == null || !wheelPairs.Any())
                    {
                        return;
                    }

                    foreach (var pair in wheelPairs)
                    {
                        Handles.color = Color.white;
                        Handles.DrawLine(pair.Wheel.Transform.position, transform.position, 1.2f);
                    }
                }

            }
#endif
        }
    }
}
