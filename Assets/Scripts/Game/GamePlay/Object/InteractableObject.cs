﻿using UnityEngine;
using System.Collections;

public class InteractableObject:MonoBehaviour {
	protected Transform target;

	protected bool isAttached;

	protected virtual void FixedUpdate() {
		if(isAttached && target != null) {
			transform.position = target.position;
			transform.rotation = target.rotation;
		}
	}

	/** Attach the object to an object */
	public void Attach(Transform other) {
		target = other;
		isAttached = true;
	}

	/** Detach the object */
	public void Detach() {
		isAttached = false;
	}
}
