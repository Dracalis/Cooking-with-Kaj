using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointMovement : RaycastController {

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
	
	CarryPassengers carryPassengers;	// Reference CarryPassengers.cs


	// Use this for initialization
	public override void Start () {
		base.Start ();

		carryPassengers = GetComponent <CarryPassengers> ();	// Get CarryPassengers.cs

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

		// If this object has CarryPassengers.cs attached, calculate movement and move passengers
		if (carryPassengers != null) {
			carryPassengers.CalculatePassengerMovement (velocity);
			carryPassengers.MovePassengers (true);
		}

		// Move this object
		transform.Translate (velocity);

		// If this object has CarryPassengers.cs attached, stop moving passengers
		if (carryPassengers != null) {
			carryPassengers.MovePassengers (false);
		}

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


	public Vector2 CalculatePlatformMovement () {

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