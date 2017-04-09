using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RaycastController : MonoBehaviour {

	// Defines the position of raycasts on the player, for detecting collisions
	[Header("Collision Detection")]
	[Tooltip("The game object will collide with objects on this layer.")]
	public LayerMask collisionMask;
	public const float distanceBetweenRays = 0.25f;
	[Tooltip("The number of raycasts that will be sent horizontally from the sides of the object.")]
	public int horizontalRayCount = 4;
	[HideInInspector]
	public float horizontalRaySpacing;
	[Tooltip("The distance that the raycasts will be drawn inside from the edges of of the BoxCollider2D.")]
	public float skinWidth = 0.125f;
	[Tooltip("The number of raycasts that will be sent vertically from the top and bottom of the object.")]
	public int verticalRayCount = 4;
	[HideInInspector]
	public float verticalRaySpacing;

	
	[HideInInspector]
	public new BoxCollider2D collider; // NOTE: This "new" declaration might not be correct
	public RaycastOrigins raycastOrigins;


	// Use this for initialization
	public virtual void Start () {
		collider = GetComponent <BoxCollider2D> ();
		CalculateRaySpacing ();
	}


	// Sets the origin points of the raycasts on the player
	public void UpdateRaycastOrigins () {
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);		// Brings the raycast origins into the collider boundary

		raycastOrigins.bottomLeft = new Vector2 (bounds.min.x, bounds.min.y);
		raycastOrigins.bottomRight = new Vector2 (bounds.max.x, bounds.min.y);
		raycastOrigins.topLeft = new Vector2 (bounds.min.x, bounds.max.y);
		raycastOrigins.topRight = new Vector2 (bounds.max.x, bounds.max.y);
	}


	// Sets the spacing of the collision detection raycasts
	public void CalculateRaySpacing () {
		Bounds bounds = collider.bounds;
		bounds.Expand (skinWidth * -2);

		float boundsWidth = bounds.size.x;
		float boundsHeight = bounds.size.y;

		horizontalRayCount = Mathf.RoundToInt (boundsHeight / distanceBetweenRays);
		verticalRayCount = Mathf.RoundToInt (boundsWidth / distanceBetweenRays);

		horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
	}


	public struct RaycastOrigins {
		public Vector2 topLeft,	topRight;
		public Vector2 bottomLeft, bottomRight;
	}
}