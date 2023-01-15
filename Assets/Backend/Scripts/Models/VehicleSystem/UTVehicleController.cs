using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

using GLShared.General.Enums;
using GLShared.General.ScriptableObjects;
using GLShared.General.Interfaces;
using GLShared.General.Components;
using GLShared.General.Signals;
using GLShared.General.Models;
using GLShared.Networking.Components;
using GLShared.General;

namespace Backend.Scripts.Models
{
    public abstract class UTVehicleController : MonoBehaviour, IVehicleController
    {
        private const float BRAKE_FORCE_OPPOSITE_INPUT_AND_FORCE_MULTIPLIER = 0.1f;
        private const float BRAKE_FORCE_NO_INPUTS_MULTIPLIER = 0.25f;

        [Inject(Id = "mainRig")] protected Rigidbody rig;
        [Inject] protected readonly SignalBus signalBus;
        [Inject] protected readonly IEnumerable<IVehicleAxle> allAxles;
        [Inject] protected readonly GameParameters gameParameters;
        [Inject] protected readonly IPlayerInputProvider inputProvider;
        [Inject] protected readonly VehicleStatsBase vehicleStats;
        [Inject] protected readonly DiContainer container;
        [Inject] protected readonly PlayerEntity playerEntity;


        [SerializeField] protected Transform centerOfMass;
        [SerializeField] protected VehicleType vehicleType = VehicleType.Car;
        [SerializeField] protected float maxSlopeAngle = 45f;
        [SerializeField] protected AnimationCurve forwardPowerCurve;
        [SerializeField] protected AnimationCurve backwardPowerCurve;
        [SerializeField] protected bool doesGravityDamping = true;
        [SerializeField] protected LayerMask wheelsCollisionDetectionMask;
        [SerializeField] protected bool runPhysics = true;

        [Header("Force apply points")]
        [SerializeField] protected ForceApplyPoint brakesForceApplyPoint = ForceApplyPoint.WheelConstraintUpperPoint;
        [SerializeField] protected ForceApplyPoint accelerationForceApplyPoint = ForceApplyPoint.WheelHitPoint;

        protected bool hasAnyWheels;
        protected bool hasTurret;

        protected float currentSpeed;
        protected float absoluteInputY;
        protected float absoluteInputX;
        protected float maxForwardSpeed;
        protected float maxBackwardsSpeed;
        protected float currentMaxForwardSpeed;
        protected float currentMaxBackwardSpeed;
        protected float currentSpeedRatio;
        protected float signedInputY;

        protected int allWheelsAmount;

        #region Computed variables
        protected bool isBrake;
        protected float inputY;
        protected float currentMaxSpeedRatio = 0;
        protected float currentDriveForce = 0;
        protected float currentLongitudalGrip;
        protected float forwardForce;
        protected float turnForce;
        protected float verticalAngle;
        protected float horizontalAngle;

        protected bool isUpsideDown = false;
        protected bool isMovingInDirectionOfInput = true;

        protected Vector3 wheelVelocityLocal;
        #endregion 

        protected IEnumerable<IPhysicsWheel> allGroundedWheels;
        protected IEnumerable<IPhysicsWheel> allWheels;

        public VehicleType VehicleType => vehicleType;
        public IEnumerable<IVehicleAxle> AllAxles => allAxles;
        public bool HasAnyWheels => hasAnyWheels;
        public float CurrentSpeed => currentSpeed;
        public float CurrentSpeedRatio => currentSpeedRatio;
        public float AbsoluteInputY => absoluteInputY;
        public float AbsoluteInputX => absoluteInputX;
        public float SignedInputY => signedInputY;
        public float MaxForwardSpeed => maxForwardSpeed;
        public float MaxBackwardsSpeed => maxBackwardsSpeed;
        public float HorizontalAngle => horizontalAngle;
        public bool DoesGravityDamping => doesGravityDamping;
        public bool IsUpsideDown => isUpsideDown;
        public bool HasTurret => hasTurret;
        public bool RunPhysics => runPhysics;
        public LayerMask WheelsCollisionDetectionMask => wheelsCollisionDetectionMask;
        public ForceApplyPoint BrakesForceApplyPoint => brakesForceApplyPoint;
        public ForceApplyPoint AccelerationForceApplyPoint => accelerationForceApplyPoint;

        public IEnumerable<IPhysicsWheel> AllWheels => allWheels;
        public float GetCurrentMaxSpeed()
        {
            return absoluteInputY == 0 ? 0 : (signedInputY > 0 ? currentMaxForwardSpeed : currentMaxBackwardSpeed);
        }

        public virtual void Initialize()
        {
            SetupRigidbody();

            maxForwardSpeed = forwardPowerCurve.keys[^1].time;
            maxBackwardsSpeed = backwardPowerCurve.keys[^1].time;

            hasAnyWheels = allAxles.Any() && allAxles.Where(axle => axle.HasAnyWheelPair && axle.HasAnyWheel).Any();
            allWheels = GetAllWheelsInAllAxles().ToArray();
            allWheelsAmount = allWheels.Count();

            hasTurret = container.TryResolve<ITurretController>() != null;

            signalBus.Subscribe<PlayerSignals.OnPlayerSpawned>(OnPlayerSpawned);

        }

        private void OnPlayerSpawned(PlayerSignals.OnPlayerSpawned OnPlayerSpawned)
        {
            if (OnPlayerSpawned.PlayerProperties.User.Name == playerEntity.Username)
            {
                gameObject.name = "(" + OnPlayerSpawned.PlayerProperties.PlayerVehicleName + ")Player '" + playerEntity.Username + "'";
                signalBus.Fire(new PlayerSignals.OnPlayerInitialized()
                {
                    PlayerProperties = playerEntity.Properties,
                    InputProvider = inputProvider,
                    VehicleStats = vehicleStats,
                });
            }
        }

        public virtual void SetupRigidbody()
        {
            rig.mass = vehicleStats.Mass;
            rig.drag = vehicleStats.Drag;
            rig.angularDrag = vehicleStats.AngularDrag;

            if (centerOfMass != null)
            {
                rig.centerOfMass = centerOfMass.localPosition;
            }
        }

        protected void CalculateVehicleAngles()
        {
            verticalAngle = 90f - Vector3.Angle(Vector3.up, transform.forward);
            horizontalAngle = 90f - Vector3.Angle(Vector3.up, transform.right);
        }

        protected virtual void FixedUpdate()
        {
            CalculateVehicleAngles();
            CalculateVehicleMaxVelocity();

            allGroundedWheels = GetGroundedWheelsInAllAxles().ToArray();
            isUpsideDown = CheckUpsideDown();
            isMovingInDirectionOfInput = Mathf.Sign(transform.InverseTransformDirection(rig.velocity).z) == Mathf.Sign(inputProvider.Vertical);
        }

        protected virtual void Update()
        {
            if (inputProvider != null)
            {
                isBrake = inputProvider.Brake;
                inputY = inputProvider.RawVertical == 0 ? 0 : inputProvider.Vertical;

                absoluteInputY = inputProvider.AbsoluteVertical;
                absoluteInputX = inputProvider.AbsoluteHorizontal;

                signedInputY = inputProvider.SignedVertical;
            }
        }

        protected void CalculateVehicleMaxVelocity()
        {
            currentMaxSpeedRatio = 1f - Mathf.Max(Mathf.Min((verticalAngle / maxSlopeAngle), 1f), 0f);
            currentMaxForwardSpeed = currentMaxSpeedRatio * maxForwardSpeed;
            currentMaxBackwardSpeed = currentMaxSpeedRatio * maxBackwardsSpeed;
        }

        protected void SetCurrentSpeed()
        {
            currentSpeed = rig.velocity.magnitude * gameParameters.SpeedMultiplier;
            float maxSpeed = GetCurrentMaxSpeed();
            currentSpeedRatio = maxSpeed != 0 ? currentSpeed / maxSpeed : 0f;
        }

        protected bool CheckUpsideDown()
        {
            return !allGroundedWheels.Any() || transform.up.y <= 0.2f;
        }

        protected void EvaluateDriveParams()
        {
            if (inputProvider.RawVertical == 0f)
            {
                currentDriveForce = 0f;
            }
            else
            {
                currentDriveForce = inputProvider.RawVertical > 0f ? forwardPowerCurve.Evaluate(currentSpeed) : backwardPowerCurve.Evaluate(currentSpeed);
            }
        }

        protected void Accelerate()
        {
            if (inputProvider.RawVertical == 0 || isBrake)
            {
                return;
            }

            foreach (var axle in allAxles)
            {
                if (axle.CanDrive && !isBrake && currentSpeed < GetCurrentMaxSpeed())
                {
                    var groundedWheels = axle.GroundedWheels;

                    if (!groundedWheels.Any())
                    {
                        continue;
                    }

                    foreach (var wheel in groundedWheels)
                    {
                        if (wheel is UTWheel)
                        {
                            if (wheel.HitInfo.NormalAndUpAngle <= gameParameters.MaxWheelDetectionAngle)
                            {
                                wheelVelocityLocal = wheel.Transform.InverseTransformDirection(rig.GetPointVelocity(wheel.UpperConstraintPoint));

                                forwardForce = inputY * currentDriveForce * Mathf.Max(currentMaxSpeedRatio, 0.6f);
                                turnForce = wheelVelocityLocal.x * currentDriveForce;

                                Vector3 acceleratePoint = wheel.ReturnWheelPoint(accelerationForceApplyPoint);

                                rig.AddForceAtPosition((forwardForce * wheel.Transform.forward), acceleratePoint);
                                rig.AddForceAtPosition((turnForce * -wheel.Transform.right), wheel.UpperConstraintPoint);
                            }
                        }
                        else
                        {
                            wheelVelocityLocal = wheel.Transform.InverseTransformDirection(rig.GetPointVelocity(wheel.UpperConstraintPoint));

                            forwardForce = inputY * currentDriveForce * 3f;
                            turnForce = wheelVelocityLocal.x * currentDriveForce;

                            rig.AddForceAtPosition((forwardForce * wheel.Transform.up), wheel.Transform.position);
                            rig.AddForceAtPosition((turnForce * -wheel.Transform.right), wheel.UpperConstraintPoint);
                        }
                    }
                }
            }
        }

        protected void Brakes()
        {
            if (!allGroundedWheels.Any())
            {
                return;
            }

            currentLongitudalGrip = isBrake ? 1f : (inputProvider.RawVertical != 0f ?
               (isMovingInDirectionOfInput ? 0f : BRAKE_FORCE_OPPOSITE_INPUT_AND_FORCE_MULTIPLIER)
               : BRAKE_FORCE_NO_INPUTS_MULTIPLIER);

            if (inputProvider.RawVertical == 0 || isBrake || !isMovingInDirectionOfInput)
            {
                float forceMultiplier = isBrake ? 0.2f : 0.7f;

                foreach (var wheel in allGroundedWheels)
                {
                    if (wheel is UTWheel)
                    {
                        Vector3 brakesPoint = wheel.ReturnWheelPoint(brakesForceApplyPoint);

                        Vector3 forwardDir = wheel.Transform.forward;
                        Vector3 tireVel = rig.GetPointVelocity(brakesPoint);

                        float steeringVel = Vector3.Dot(forwardDir, tireVel);
                        float desiredVelChange = -steeringVel * currentLongitudalGrip;
                        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                        rig.AddForceAtPosition(desiredAccel * (wheel.TireMass * forceMultiplier) * forwardDir, brakesPoint);
                    }

                }
            }
        }
        protected virtual void CustomGravityLogic()
        {
        }

        protected IEnumerable<IPhysicsWheel> GetGroundedWheelsInAllAxles()
        {
            var result = new List<IPhysicsWheel>();
            if (allAxles.Any())
            {
                foreach (var axle in allAxles)
                {
                    if (axle.GroundedWheels.Any())
                    {
                        result.AddRange(axle.GroundedWheels);
                    }
                }
            }
            return result;
        }

        protected IEnumerable<IPhysicsWheel> GetAllWheelsInAllAxles()
        {
            var result = new List<IPhysicsWheel>();
            if (allAxles.Any())
            {
                foreach (var axle in allAxles)
                {
                    if (axle.HasAnyWheelPair && axle.HasAnyWheel)
                    {
                        result.AddRange(axle.AllWheels);
                    }
                }
            }
            return result;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (rig == null)
            {
                rig = GetComponent<Rigidbody>();
            }

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(rig.worldCenterOfMass, 0.2f);
#endif
        }
    }
}
