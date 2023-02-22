using Backend.Scripts.Models;
using GLShared.General.Enums;
using GLShared.General.Interfaces;
using UnityEngine;
using System.Linq;

namespace Backend.Scripts.Components
{
    public class UTNormalAxle : UTBackendAxle
    {
        [Header("Antiroll")]
        [SerializeField] protected bool applyAntiroll;
        [SerializeField] protected float antiRollForce = 0f;

        private IPhysicsWheel leftAntirolled, rightAntirolled;

        public override void Initialize()
        {
            base.Initialize();
            leftAntirolled = GetAllWheelsOfAxis(DriveAxisSite.Left).First();
            rightAntirolled = GetAllWheelsOfAxis(DriveAxisSite.Right).First();
        }

        public override void SetSteerAngle(float angleLeftAxis, float angleRightAxis)
        {
            foreach (var pair in wheelPairs)
            {
                pair.Wheel.SteerAngle = pair.Axis == DriveAxisSite.Left ? angleLeftAxis : angleRightAxis;
            }
        }

        private void FixedUpdate()
        {
            if (!wheelPairs.Any() || controller == null || controller.IsUpsideDown)
            {
                return;
            }

            groundedWheels = GetGroundedWheels();
            isAxleGrounded = CheckAxleGrounded();

            if (applyAntiroll)
            {
                CalculateAndApplyAntiroll();
            }

        }

        private void CalculateAndApplyAntiroll()
        {
            float antiRollFinalForce = (leftAntirolled.CompressionRate - rightAntirolled.CompressionRate) * antiRollForce;
            if (leftAntirolled.IsGrounded)
            {
                rig.AddForceAtPosition(leftAntirolled.Transform.up * antiRollFinalForce,
                          leftAntirolled.UpperConstraintPoint);
            }

            if (rightAntirolled.IsGrounded)
            {
                rig.AddForceAtPosition(rightAntirolled.Transform.up * -antiRollFinalForce,
                 rightAntirolled.UpperConstraintPoint);
            }
        }
    }
}
