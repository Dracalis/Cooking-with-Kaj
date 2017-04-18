using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarryPassengers : RaycastController {

	// Variables
	[Tooltip ("Objects on this layer can be moved by this game object.")]
	public LayerMask passengerMask;

	List <PassengerMovement> passengerMovement;
	Dictionary <Transform, Controller2D> passengerDictionary = new Dictionary <Transform, Controller2D> ();

	void Update () {
		UpdateRaycastOrigins ();
	}

	// Moves the passengers
	public void MovePassengers (bool beforeMovePlatform) {
		foreach (PassengerMovement passenger in passengerMovement) {
			if (!passengerDictionary.ContainsKey (passenger.transform)) {
				passengerDictionary.Add (passenger.transform, passenger.transform.GetComponent <Controller2D>());
			}

			if (passenger.moveBeforePlatform == beforeMovePlatform) {
				passengerDictionary [passenger.transform].Move (passenger.velocity, passenger.standingOnPlatform);
			}
		}
	}

	// Calculates passenger movement
	public void CalculatePassengerMovement (Vector2 velocity) {
		HashSet <Transform> movedPassengers = new HashSet <Transform> ();
		passengerMovement = new List <PassengerMovement> ();

		float directionX = Mathf.Sign (velocity.x);
		float directionY = Mathf.Sign (velocity.y);

		// Vertically moving platform
		if (velocity.y != 0) {
			float rayLength = Mathf.Abs (velocity.y) + skinWidth;
			
			for (int i = 0; i < verticalRayCount; i ++) {
				Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
				rayOrigin += Vector2.right * (verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, passengerMask);
				Debug.DrawRay (rayOrigin, Vector2.up * directionY, Color.red);

				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains (hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = (directionY == 1) ? velocity.x : 0;
						float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

						passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), directionY == 1, true));
					}
				}
			}
		}


		// Horizontally moving platform
		if (velocity.x != 0) {
			float rayLength = Mathf.Abs (velocity.x) + skinWidth;

			for (int i = 0; i < horizontalRayCount; i ++) {
				Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
				rayOrigin += Vector2.up * (horizontalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains (hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
						float pushY = -skinWidth;

						passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), false, true));
					}
				}
			}
		}


		// Passenger on top of a horizontally- or downward-moving platform
		if (directionY == -1 || velocity.y == 0 && velocity.x != 0) {
			float rayLength = skinWidth * 2;
			
			for (int i = 0; i < verticalRayCount; i ++) {
				Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
				RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
				
				if (hit && hit.distance != 0) {
					if (!movedPassengers.Contains (hit.transform)) {
						movedPassengers.Add (hit.transform);
						float pushX = velocity.x;
						float pushY = velocity.y;
						
						passengerMovement.Add (new PassengerMovement (hit.transform, new Vector2 (pushX, pushY), true, false));
					}
				}
			}
		}
	}


	struct PassengerMovement {
		public Transform transform;
		public Vector2 velocity;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement (Transform _transform, Vector2 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform) {
			transform = _transform;
			velocity = _velocity;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}
}