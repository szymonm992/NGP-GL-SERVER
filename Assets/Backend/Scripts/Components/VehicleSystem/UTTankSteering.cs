using GLShared.General.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class UTTankSteering : MonoBehaviour
    {
        [Inject(Id = "mainRig")] private Rigidbody rig;
        [Inject] private readonly IVehicleController suspensionController;
        [Inject] private readonly IPlayerInputProvider inputProvider;

        [SerializeField] private float steerForce = 5f;

        private float steerInput;
        private float currentSteerForce;

        public float SteerForce => steerForce;

        public void SetSteeringInput(float input)
        {
            steerInput = inputProvider.AbsoluteVertical != 0 ? input * inputProvider.SignedVertical : input;
        }

        private void Update()
        {
            if (suspensionController != null)
            {
                SetSteeringInput(inputProvider.Horizontal);
            }
        }


        private void FixedUpdate()
        {
            if (!suspensionController.RunPhysics && (steerInput == 0 || suspensionController.IsUpsideDown))
            {
                return;
            }

            currentSteerForce = steerForce;

            if (inputProvider.CombinedInput > 1)
            {
                currentSteerForce *= (1.0f / Mathf.Sqrt(2));
            }

            foreach (var axle in suspensionController.AllAxles)
            {
                if (axle.CanSteer)
                {
                    var wheelsInAxle = axle.AllWheels;
                    foreach (var wheel in wheelsInAxle)
                    {
                        if (wheel.IsGrounded)
                        {
                            int invertValue = axle.InvertSteer ? -1 : 1;
                            rig.AddForceAtPosition(invertValue * currentSteerForce * steerInput * rig.transform.right, wheel.HitInfo.Point, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }
    }
}
