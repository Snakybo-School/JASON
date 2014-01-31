﻿using UnityEngine;
using System.Collections;

public class PlayerCamera:MonoBehaviour {
	public float damping = 0.15f;
	
	public Transform target;
	
	private Vector3 velocity = Vector3.zero;
	
	void Update() {
		if(target != null) {
			Vector3 point = camera.WorldToViewportPoint(target.position);
			Vector3 delta = target.position - camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z));
			Vector3 destination = transform.position + delta;
			
			transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, damping);
		}
		
	}
}
