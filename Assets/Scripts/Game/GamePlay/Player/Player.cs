﻿using UnityEngine;
using System;
using System.Collections;

public class Player:MonoBehaviour {
	[SerializeField] protected GameObject scrollingCombatText;

	private Boss currentBoss;

	private Animator playerAnimation;
	private PlayerCamera playerCamera;
	private PlayerCombat playerCombat;

	private Transform pushablePosition;
	private Transform throwablePosition;
	
	private Vector3 targetPosition;
	
	private PushableObject attachedPushable;
	private ThrowableObject attachedThrowable;

	private float previousX;
	
	private int mask = ~(1 << 8);
	
	private bool dataInstanceFound;
	private bool isHit;
	private bool isInBossRoom;
	
	void Start() {
		playerCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<PlayerCamera>();
		playerCombat = GetComponent<PlayerCombat>();
		playerAnimation = GetComponentInChildren<Animator>();

		targetPosition = transform.position;
		
		throwablePosition = transform.FindChild("Throwable Position");
		pushablePosition = transform.FindChild("Pushable Position");

		previousX = transform.position.x;
	}
	
	void Update() {
		if(!dataInstanceFound) {
			if(GameObject.FindGameObjectWithTag("Global Manager") == null) {
				return;
			} else {
				dataInstanceFound = true;
			}
		}
	}
	
	void FixedUpdate() {
		rigidbody.velocity = Vector3.zero;
		
		HandlePickedUpObject();
		CheckForInput();
		
		if(Vector3.Distance(transform.position, targetPosition) > 0.5f) {
			playerAnimation.SetBool("IsRunning", true);

			if(!isInBossRoom)
				playerCamera.CameraDistance = 10;

			transform.position = Vector3.MoveTowards(transform.position, targetPosition, PlayerData.Instance.RunSpeed * Time.deltaTime); 
			
			if(attachedPushable == null) {
				Vector3 lookPosition = targetPosition - transform.position;
				Quaternion lookRotation = Quaternion.identity;
				
				if(lookPosition != Vector3.zero)
					lookRotation = Quaternion.LookRotation(lookPosition);
				
				lookRotation.x = 0;
				lookRotation.z = 0;
				
				if(transform.position != targetPosition)
					transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 30);
			}
		} else {
			if(!isInBossRoom)
				playerCamera.CameraDistance = -5;

			playerAnimation.SetBool("IsRunning", false);
		}

		if(transform.position.x < previousX)
			playerCamera.CameraDistance = 10;

		previousX = transform.position.x;
	}

	/** Pickup an object */
	public IEnumerator Pickup(GameObject obj) {
		if(attachedPushable != null || attachedThrowable != null) {
			Drop();
		} else {
			Move(obj.transform.position);

			while(Vector3.Distance(transform.position, obj.transform.position) > 2.5f)
				yield return new WaitForSeconds(0.5f);

			switch(obj.tag) {
			case "ThrowableObject":
				attachedThrowable = obj.GetComponent<ThrowableObject>();
				attachedThrowable.collider.enabled = false;
				break;
			case "PushableObject":
				playerCombat.WeaponCollisionArea.collider.enabled = false;
				attachedPushable = obj.GetComponent<PushableObject>();
				transform.LookAt(attachedPushable.transform.position);
				transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);
				break;
			}
		}
	}

	/** Drop the picked up object */
	public void Drop() {
		if(attachedPushable != null) {
			attachedPushable.rigidbody.mass = 1000;
			attachedPushable.rigidbody.drag = 1000;
			attachedPushable.rigidbody.angularDrag = 1000;
		}
		
		attachedThrowable = null;
		attachedPushable = null;

		playerCombat.WeaponCollisionArea.collider.enabled = true;
	}

	/** Throw the picked up object */
	public void ThrowObject() {
		if(attachedThrowable != null) {
			attachedThrowable.Thrown = true;
			Drop();
		}
	}

	/** Damage the player */
	public void Damage(int amount, float stunTime, bool crit) {
		playerAnimation.SetBool("GettingHit", true);
		PlayerData.Instance.Health -= amount;
		
		DisplayCombatText(amount.ToString(), Color.yellow, crit);
		
		isHit = true;

		if(PlayerData.Instance.Health <= 0) {
			PlayerData.Instance.Reset();

			if(!isInBossRoom) {
				Application.LoadLevel(Application.loadedLevel);
			} else {
				currentBoss.Reset();
				transform.position = GameObject.Find("Player Spawn").transform.position;
				targetPosition = transform.position;
			}
		}
		
		StartCoroutine(DamageDelay(stunTime));
	}
	
	protected void DisplayCombatText(string text, Color color, bool crit) {
		Vector3 position = transform.position;
		position.y += 3.5f;
		
		TextMesh popup = (Instantiate(Resources.Load("Prefabs/Misc/Scrolling Combat Text"), position, Quaternion.identity) as GameObject).GetComponent<TextMesh>();
		
		if(crit) {
			Vector3 ls = popup.transform.localScale;
			
			ls.x *= 2;
			ls.y *= 2;
			ls.z *= 2;
			
			popup.transform.localScale = ls;
		}
		
		popup.transform.parent = this.transform;
		
		popup.text = text;
		popup.color = color;
	}

	/** Handle the picked up object */
	private void HandlePickedUpObject() {
		if(attachedThrowable != null) {
			attachedThrowable.transform.position = throwablePosition.position;
			attachedThrowable.transform.rotation = throwablePosition.rotation;
		} else if(attachedPushable != null) {
			if(attachedPushable.transform.position == pushablePosition.position) {
				attachedPushable.rigidbody.mass = 1000;
				attachedPushable.rigidbody.drag = 1000;
				attachedPushable.rigidbody.angularDrag = 1000;
			} else {
				attachedPushable.rigidbody.mass = 1;
				attachedPushable.rigidbody.drag = 1;
				attachedPushable.rigidbody.angularDrag = 0.5f;
			}
			
			attachedPushable.rigidbody.velocity = -(attachedPushable.transform.position - pushablePosition.position).normalized * 5;

			if(Vector3.Distance(transform.position, attachedPushable.transform.position) > 5)
				Drop();
		}
	}

	/** Check for input */
	private void CheckForInput() {
		RaycastHit hit;
		Ray ray;
		
		if(Input.touchCount == 0) {
			if(Input.GetMouseButtonDown(0)) {
				if(Input.mousePosition.x <= 176 && Input.mousePosition.y <= 50)
					return;
				
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				Physics.Raycast(ray, out hit, 100, mask);
				
				HandleInput(hit);
			}
		} else {
			Touch touch = Input.GetTouch(0);
			
			if(touch.position.x <= 176 && touch.position.y <= 50)
				return;
			
			ray = Camera.main.ScreenPointToRay(touch.position);
			Physics.Raycast(ray, out hit, 100, mask);
			
			HandleInput(hit);
		}
	}

	private void HandleInput(RaycastHit hit) {
		if(hit.collider == null)
			return;
		
		if(playerCombat.Defending)
			return;

		switch(hit.transform.tag) {
		case "Floor":
			Move(hit.point);
			break;
		case "Enemy":
		case "Boss":
			playerCombat.StartAttack(hit.transform.gameObject);
			break;
		case "ThrowableObject":
		case "PushableObject":
			StartCoroutine(Pickup(hit.collider.gameObject));
			break;
		default:
			Move(hit.point);
			break;
		}
	}

	/** Move the player */
	private void Move(Vector3 position) {
		playerCombat.Target = null;
		targetPosition = new Vector3(position.x, 1, position.z);
	}

	/** The damage delay of the player */
	private IEnumerator DamageDelay(float stunTime){
		yield return new WaitForSeconds(stunTime);
		
		isHit = false;
		
		playerAnimation.SetBool("GettingHit", false);
	}

	/** Set and/or get the current boss */
	public Boss CurrentBoss {
		set { currentBoss = value; }
		get { return currentBoss; }
	}

	/** Get the animator component of the player */
	public Animator PlayerAnimation {
		get { return playerAnimation; }
	}

	/** Get the camera component of the player */
	public PlayerCamera PlayerCamera {
		get { return playerCamera; }
	}

	/** Get the combat component of the player */
	public PlayerCombat PlayerCombat {
		get { return playerCombat; } 
	}
	
	/** Get the pushable position */
	public Transform PushablePosition {
		get { return pushablePosition; }
	}

	/** Get the throwable position */
	public Transform ThrowablePosition {
		get { return throwablePosition; }
	}

	/** Set and/or get the target position */
	public Vector3 TargetPosition {
		set { targetPosition = value; }
		get { return targetPosition; }
	}

	/** Set and/or get the attached pushable object */
	public PushableObject AttachedPushable {
		set { attachedPushable = value; }
		get { return attachedPushable; }
	}

	/** Set and/or get the attached throwable object */
	public ThrowableObject AttachedThrowable {
		set { attachedThrowable = value; }
		get { return attachedThrowable; }
	}
	
	/** Get wheter or not the player is hit */
	public bool Hit { 
		get { return isHit; }
	}

	/** Set and/or get wheter or not the player is in a boss room */
	public bool InBossRoom {
		set { isInBossRoom = value; }
		get { return isHit; }
	}
}
