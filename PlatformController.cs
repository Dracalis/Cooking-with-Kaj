using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController {

	[Tooltip ("Objects on this layer can be moved by the platform.")]
	public LayerMask passengerMask;
	
	// Waypoint Positions
	[Header ("Waypoint Positions")]
	[Tooltip ("If the waypoints are cyclical, the platform will loop when it reaches the end. If not, it will reverse direction.")]
	public bool cyclical;		// If true, the waypoints will loop : if false, they reverse
	[Tooltip ("If this is true, the waypoints will be reversed as soon as the game starts, changing the way the platform travels.")]
	public bool invertedStart;	// If true, this will reverse the waypoints as soon as the game starts
	[Tooltip ("These are the coordinates, local to the platform, that it will sequentially travel.")]
	public Vector2[] localWaypoints;
	Vector2[] globalWaypoints;

	// Timing
	[Header ("Timing")]
	[Tooltip ("The higher the easing, the more gradually the platform will accelerate and decelerate. No easing means speed is constant.")]
	[Range (0f, 2f)]
	public float easeAmount;
	[Tooltip ("The speed of the platform's movement.")]
	[Range (0f, 100f)]
	public float speed;		// Speed of the platform's movement
	[Tooltip ("The time in seconds that the platform will wait before moving towards the next waypoint.")]
	[Range (0f, 10f)]
	public float waitTime;	// Wait between moving to the next waypoint

	// Colour
	[Header ("Gizmo Colour")]
	[SerializeField]
	[Tooltip ("This controls the amount of red in the colour of the waypoint gizmos.")]
	[Range (0f, 1f)]
	float red = 0.5f;
	[SerializeField]
	[Tooltip ("This controls the amount of green in the colour of the waypoint gizmos.")]
	[Range (0f, 1f)]
	float green = 0.5f;
	[SerializeField]
	[Tooltip ("This controls the amount of blue in the colour of the waypoint gizmos.")]
	[Range (0f, 1f)]
	float blue = 0.5f;
	[SerializeField]
	[Tooltip ("This controls the transparency of the waypoint gizmos.")]
	[Range (0f, 1f)]
	float alpha = 1f;
	
	// Data
	[Header ("Data")]
	[SerializeField]
	[Tooltip ("The last waypoint that the platform departed.")]
	int fromWaypointIndex;
	[Tooltip ("The percentage of the path that the platform has travelled between the current waypoints.")]
	[SerializeField]
	[Range(0f, 100f)]
	float percentageJourneyed;	// A serialized private field to show the percentage journeyed, based on percentBetweenWaypoints
	float percentBetweenWaypoints;
	float nextMoveTime;
	[Tooltip ("The time remaining, in seconds, until the platform continues to move.")]
	[SerializeField]
	float timeUntilMovement;	// A serialized private field in the inspector to show a countdown until the platform moves

	List <PassengerMovement> passengerMovement;
	Dictionary <Transform, Controller2D> passengerDictionary = new Dictionary <Transform, Controller2D> ();


	// Use this for initialization
	public override void Start () {
		base.Start ();

		globalWaypoints = new Vector2 [localWaypoints.Length];
		for (int i = 0; i < localWaypoints.Length; i ++) {
			globalWaypoints [i] = localWaypoints [i] + (Vector2) transform.position;
		}

		if (invertedStart == true) {
			System.Array.Reverse (globalWaypoints);
		}
	}

	
	// Update is called once per frame
	void Update () {
		UpdateRaycastOrigins ();

		Vector2 velocity = CalculatePlatformMovement();
		CalculatePassengerMovement (velocity);

		MovePassengers (true);
		transform.Translate (velocity);
		MovePassengers (false);

		// A serialized private field in the inspector to show a countdown until the platform moves
		if (nextMoveTime > Time.time) {
			timeUntilMovement = nextMoveTime - Time.time;
		} else {
			timeUntilMovement = 0f;
		}

		percentageJourneyed = Mathf.Round (percentBetweenWaypoints * 100);
	}


	float Ease (float x) {
		float a = easeAmount + 1;
		return Mathf.Pow (x, a) / (Mathf.Pow (x, a) + Mathf.Pow (1 - x, a));
	}


	Vector2 CalculatePlatformMovement () {

		if (Time.time < nextMoveTime) {
			return Vector2.zero;
		}

		fromWaypointIndex %= globalWaypoints.Length;
		int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
		float distanceBetweenWaypoints = Vector2.Distance (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex]);
		percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
		percentBetweenWaypoints = Mathf.Clamp01 (percentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Ease (percentBetweenWaypoints);

		Vector2 newPosition = Vector2.Lerp (globalWaypoints [fromWaypointIndex], globalWaypoints [toWaypointIndex], easedPercentBetweenWaypoints);

		if (percentBetweenWaypoints >= 1f) {
			percentBetweenWaypoints = 0f;
			fromWaypointIndex ++;

			// If the waypoints aren't cyclical, reverse the trajectory
			if (!cyclical) {
				if (fromWaypointIndex >= globalWaypoints.Length - 1) {
					fromWaypointIndex = 0;
					System.Array.Reverse (globalWaypoints);
				}
			}

			nextMoveTime = Time.time + waitTime;
		}

		return newPosition - (Vector2) transform.position;
	}


	void MovePassengers (bool beforeMovePlatform) {
		foreach (PassengerMovement passenger in passengerMovement) {
			if (!passengerDictionary.ContainsKey (passenger.transform)) {
				passengerDictionary.Add (passenger.transform, passenger.transform.GetComponent <Controller2D>());
			}

			if (passenger.moveBeforePlatform == beforeMovePlatform) {
				passengerDictionary [passenger.transform].Move (passenger.velocity, passenger.standingOnPlatform);
			}
		}
	}


	void CalculatePassengerMovement (Vector2 velocity) {
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


	void OnDrawGizmos () {
		if (localWaypoints != null) {
			Gizmos.color = new Vector4 (red, green, blue, alpha);
			float size = 0.3f;

			for (int i = 0; i < localWaypoints.Length; i ++) {
				Vector2 globalWaypointPosition = (Application.isPlaying) ? globalWaypoints [i] : localWaypoints [i] + (Vector2) transform.position;
				Gizmos.DrawLine (globalWaypointPosition - Vector2.up * size, globalWaypointPosition + Vector2.up * size);
				Gizmos.DrawLine (globalWaypointPosition - Vector2.left * size, globalWaypointPosition + Vector2.left * size);
			}
		}
	}
}
