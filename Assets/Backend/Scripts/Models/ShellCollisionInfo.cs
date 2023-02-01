using UnityEngine;

namespace Backend.Scripts.Models
{
    public struct ShellCollisionInfo
    {
        public Vector3 CollisionPoint { get; set; }
        public RaycastHit Hit { get; set; }
        public Vector3 CollisionNormal { get; set; }
        public bool IsColliding { get; set; }
        public Collider Collider { get; set; }
        public LayerMask CollisionLayerMask { get; set; }

        public ShellCollisionInfo(bool IsColliding, Vector3 CollisionPoint, Vector3 CollisionNormal, Collider Collider, LayerMask CollisionLayerMask, RaycastHit Hit)
        {
            this.CollisionPoint = CollisionPoint;
            this.Hit = Hit;
            this.CollisionNormal = CollisionNormal;
            this.IsColliding = IsColliding;
            this.Collider = Collider;
            this.CollisionLayerMask = CollisionLayerMask;
        }
    }
}
