using Backend.Scripts.Models;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using System.Linq;
using GLShared.General.Components;

namespace Backend.Scripts.Components
{
    public class UTTankController : UTVehicleController
    {
        [Inject] private readonly IEnumerable<UTIdlerWheel> idlerWheels;

        public IEnumerable<UTIdlerWheel> IdlerWheels => idlerWheels;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            CustomGravityLogic();

            if (!isUpsideDown)
            {
                EvaluateDriveParams();
                Accelerate();
                Brakes();

                SetCurrentSpeed();
            }
        }

        protected override void CustomGravityLogic()
        {
            if (!allGroundedWheels.Where(wheel => !wheel.IsIdler).Any())
            {
                rig.AddForce(Physics.gravity, ForceMode.Acceleration);
            }
            else
            {
                float angle = Vector3.Angle(transform.up, -Physics.gravity.normalized);

                if (maxSlopeAngle >= angle)
                {
                    rig.AddForce(-transform.up * Physics.gravity.magnitude, ForceMode.Acceleration);
                }
                else
                {
                    rig.AddForce(Physics.gravity, ForceMode.Acceleration);
                }
            }
        }
    }
}
