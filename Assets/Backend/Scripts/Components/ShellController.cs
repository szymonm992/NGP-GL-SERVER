using Backend.Scripts.Models;
using Backend.Scripts.ScriptableObjects;
using GLShared.General.Components;
using GLShared.General.Interfaces;
using GLShared.Networking.Components;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Zenject;

namespace Backend.Scripts.Components
{
    public class ShellController : MonoBehaviour, IShellController
    {
        private const float MAX_SHOOTING_ANGLE = 5.0f;
        private const float GRAVITY_MULTIPLIER = 0.5f;
        private const float ROTATION_TIME_OFFSET = 0.1f;
        private const float MINIMUM_CURVE_MOVEMENT_DISTANCE = 50f;

        [Inject] private readonly IShellStats shellStats;
        [Inject] private readonly SignalBus signalBus;
        [Inject] private readonly ShellEntity shellEntity;
        [Inject] private readonly ISyncManager syncManager;
        [Inject] private readonly ShellsSettings shellsSettings;

        [SerializeField] private float shellDestructionTime = 5f;

        private float gravity; //Ideally, should be constant.
        private float angle; //Vertical angle of shell flight, in radians
        private float time; //Incremented every frame using Time.deltaTime
        private float velocity; //Speed, usually same as shellConfig.Speed, but can be changed
        private Vector3 direction; //Normalized direction to the target on XZ plane
        private Vector3 startingPosition;
        public (Vector3 point, float distance) targetProperties;
        private ShellCollisionInfo collisionInfo;

        private bool isColliding = false;
        private bool hasBounced = false;

        public float Velocity => velocity;

        public void Initialize()
        {
            targetProperties = shellEntity.Properties.TargetingProperties;

            InitializeShellParameters();
            Destroy(gameObject, shellDestructionTime);
        }

        public void Dispose()
        {
            syncManager.TryDestroyingShell(shellEntity.Properties.ShellSceneIdentifier);
        }

        private void InitializeShellParameters()
        {
            startingPosition = transform.position;
            gravity = -Physics.gravity.y * shellStats.GravityMultiplier;
            var targetDir = targetProperties.point - startingPosition;

            // making it a 2d problem
            float relX = Mathf.Sqrt(targetDir.x * targetDir.x + targetDir.z * targetDir.z); // horizontal movement
            float relY = targetDir.y; // vertical movement

            // using free-fall equation to calculate possible angles
            velocity = shellStats.Speed;
            float delta = velocity * velocity * velocity * velocity - gravity * (gravity * relX * relX + 2.0f * relY * velocity * velocity); // v^4 - g(gx^2 + 2yv^2)

            // handling shells that would not reach with given speed
            if (delta < 0)
            {
                delta = 0;
                gravity = (velocity * velocity) / (relX * relX) * (Mathf.Sqrt(relX * relX + relY * relY) - relY);
            }

            // this solution always gives lower angle, and thus shortest path
            float tanAngle = (velocity * velocity - Mathf.Sqrt(delta)) / (gravity * relX);
            float shootAngle = Mathf.Atan(tanAngle);
            float maxAng = Mathf.Atan2(relY, relX) + Mathf.Deg2Rad * MAX_SHOOTING_ANGLE;

            // the angle is too big, fix gravity to change that
            if (shootAngle > maxAng)
            {
                shootAngle = maxAng;
                gravity = 2.0f * (relX * Mathf.Tan(shootAngle) - relY) / Mathf.Pow(relX / (velocity * Mathf.Cos(shootAngle)), 2.0f);
            }

            // setting all necessary values
            angle = shootAngle;
            time = 0;
            direction = new Vector3(targetDir.x, 0f, targetDir.z).normalized;
        }

        private void FixedUpdate()
        {
            if (!isColliding)
            {
                if (!hasBounced && targetProperties.distance > MINIMUM_CURVE_MOVEMENT_DISTANCE)
                {
                    ShellMovementOnCurve();
                }
                else
                {
                    ShellStraightMovement();
                }
            }
            else
            {
                CollisionDetectionLogic();
            }
        }

        public bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        private void CollisionDetectionLogic()
        {
            hasBounced = true;
            Collider collider = collisionInfo.Collider;
            int layerOfCol = collider.gameObject.layer;

            if (IsInLayerMask(layerOfCol, shellsSettings.ObstaclesAndArmorMask))
            {
                if (IsInLayerMask(layerOfCol, shellsSettings.ArmorLayerMask))
                {
                    var armorComponent = collider.gameObject.GetComponent<Armor>();
                    string idOfUser = armorComponent.OwnerUsername;

                    //TODO: Check if player is alive

                    float armorThickness = armorComponent.Thickness;
                    float angleOfHit = CalculateAngleOfHit(transform.forward, -collisionInfo.CollisionNormal);
                    
                    //TODO: Check if hit is not an ally and if so then destroy object. Otherwise deal damage

                    /*if (collidingCol.transform.root.tag != RoomManager.Instance.GetPlayer(shellConfig.shell_owner).Controller.gameObject.tag)
                    {
                        //if detected object is  in the same team
                        ShellPassingInfo spi = ToSPIObject(shellConfig, sci, angleOfHit, armorThickness, attached_comp, idOfUser);
                        shellReactions.FurtherLogic(spi);
                    }
                    else
                        HandleDecalAndDestructionLogic(0, sci.CollisionNormal, idOfUser);*/
                }
            }
            else
            {
                //This case should never happen btw
                Debug.LogError("Detected unhandled mask!");
            }
        }

        private void ShellStraightMovement()
        {
            var desiredPos = transform.position + (transform.forward * (shellStats.Speed * Time.deltaTime));
            collisionInfo = ReturnCollisionInfo(desiredPos);
            transform.position = collisionInfo.CollisionPoint;
            isColliding = collisionInfo.IsColliding;
        }

        private void ShellMovementOnCurve()
        {
            var positionOnCurve = GetShellPositionAt(time);
            collisionInfo = ReturnCollisionInfo(positionOnCurve);
            transform.position = Vector3.MoveTowards(transform.position, collisionInfo.CollisionPoint, Time.deltaTime * velocity);

            if (isColliding)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(GetShellPositionAt(time + ROTATION_TIME_OFFSET) - transform.position);
            time += Time.deltaTime;
        }
        
        private Vector3 GetShellPositionAt(float time)
        {
            //calculating relative position in 2d plane.
            float relativeX = velocity * time * Mathf.Cos(angle);
            float relativeY = velocity * time * Mathf.Sin(angle) - GRAVITY_MULTIPLIER * gravity * time * time;

            var finalPos = startingPosition;
            finalPos += direction * relativeX;
            finalPos.y += relativeY;

            return finalPos;
        }

        private ShellCollisionInfo ReturnCollisionInfo(Vector3 desiredPosition)
        {
            var direction = desiredPosition - transform.position;
            isColliding = Physics.Raycast(new Ray(transform.position, direction), out RaycastHit hit, direction.magnitude, shellsSettings.ObstaclesAndArmorMask);

            if (isColliding)
            {
                //Debug.DrawRay(shellray.origin, shellray.direction*shellhit.distance,Color.green);

                return new ()
                {
                    IsColliding = true,
                    CollisionPoint = hit.point,
                    CollisionNormal = hit.normal,
                    Collider = hit.collider,
                    CollisionLayerMask = hit.collider.gameObject.layer,
                    Hit = hit,
                };
            }
            else
            {
                //Debug.DrawRay(shellray.origin, shellray.direction* lengthOfChecking, Color.red);
                return new (false, desiredPosition, Vector3.zero, null, new LayerMask(), hit);
            }
        }

        private void RicochetLogic()
        {
            Vector3 reflectedAngle = Vector3.Reflect(transform.forward, collisionInfo.CollisionNormal);
            transform.rotation = Quaternion.LookRotation(reflectedAngle);
            //after executing this part we reset the collision detection so the ShellFreeMovement part is starting to be executed
            isColliding = false;
        }

        private float CalculateAngleOfHit(Vector3 shellDirection, Vector3 collisionNormal)
        {
            return Vector3.Angle(shellDirection, collisionNormal);
        }

        private float CalculateNominalThickness(float armorThickness, float angle)
        {
            return armorThickness / Mathf.Cos(angle * Mathf.Deg2Rad);
        }
    }
}
