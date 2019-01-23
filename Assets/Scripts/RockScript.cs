using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockScript : MonoBehaviour {

	public enum state {
		FIXED,
		HELD,
		PUSHED,
		SCRIPTMOVE,
	}

	public bool isBig;
	bool superCharged;
	public state currentState = state.FIXED;

	private float timePushed = 0;
	public int highlighted_ = 0;

	[Header("Properties")]
	[SerializeField] AudioClip destroySound;
	public Sprite[] spritePool;
	public bool randomSprite;

	public float pushSpeed;
	public float grabbedSpeed;
	int projectileLayer = 0;
	int noColLayer = 0;
	public GameObject destroyParticles;
	public SpriteRenderer[] selectorRenderers;
	public float smallForceRadius;
	public float constantForceRadius;
	public float smallForce;
	public float constForce;
	public float destroyTime;

	[Header("Particles")]
	public GameObject jumpDestroyParticles;
	public GameObject superChargeFire;

    private Color startColor;
	private Rigidbody2D rb2d;
	private SpriteRenderer sr;

	private KinematicPlayer owner;

	bool onlyOnce = false;
	
	[HideInInspector]
	public bool destroyOther;

	Collider2D[] aimAssistTarget = new Collider2D[16];
	public ContactFilter2D contactFilter;

	ContactFilter2D rockFilter;

	// public so we can do physics2d.ignore collision in the player punch function and get the offset of the block
	[HideInInspector]
	public BoxCollider2D c2d;

	[HideInInspector]
	public DestroyObject attachedObject;

	public int highlighted {
		get {
			return highlighted_;
		}
		set {
            selectorRenderers[0].enabled = false;
            selectorRenderers[1].enabled = false;
			if (value != 0) {
                selectorRenderers[value - 1].enabled = true;
			}
			highlighted_ = value;
		}
	}

	void Awake () {
		destroyOther = true;
		sr = GetComponent<SpriteRenderer>();
		c2d = GetComponent<BoxCollider2D>();
		rb2d = GetComponent<Rigidbody2D>();

		projectileLayer = LayerMask.NameToLayer("Ground");
		noColLayer = LayerMask.NameToLayer("NoColLayer");

		rockFilter.useLayerMask = true;
		rockFilter.layerMask = ~LayerMask.GetMask("PlayerLayer", "NoColLayer");

		if (randomSprite) sr.sprite = spritePool[Random.Range(0, spritePool.Length)];

		attachedObject = GetComponent<DestroyObject>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (currentState == state.HELD)
		{
			if (!owner)
			{
				Destroy(gameObject);
				return;
			}

			Vector3 centerPosition = c2d.offset;
			centerPosition += transform.position;

			Vector2 direction = (owner.grabbedRocksPosition.position - centerPosition).normalized;
			float distance = Vector2.Distance(centerPosition, owner.grabbedRocksPosition.position);
			rb2d.velocity = direction * grabbedSpeed * distance;
		}
		
		if (currentState == state.PUSHED)
		{
			Vector3 centerPosition = c2d.offset;
			centerPosition += transform.position;

			// ! needs to work without owner !
			int smallForceCount = Physics2D.OverlapCircle(centerPosition, smallForceRadius, contactFilter, aimAssistTarget);

			for (int i = 0; i < smallForceCount; i++)
			{
				if (owner)
				{
					if (aimAssistTarget[i].CompareTag("Player") && !onlyOnce && aimAssistTarget[i].gameObject != owner.gameObject)
					{
						rb2d.AddForce((aimAssistTarget[i].transform.position - centerPosition).normalized * smallForce, ForceMode2D.Impulse);
						rb2d.velocity = rb2d.velocity.normalized * pushSpeed;
						onlyOnce = true;
					}
				}
				else
				{
					if (aimAssistTarget[i].CompareTag("Player") && !onlyOnce)
					{
						rb2d.AddForce((aimAssistTarget[i].transform.position - centerPosition).normalized * smallForce, ForceMode2D.Impulse);
						rb2d.velocity = rb2d.velocity.normalized * pushSpeed;
						onlyOnce = true;
					}
				}
			}

			int conForceCount = Physics2D.OverlapCircle(centerPosition, constantForceRadius, contactFilter, aimAssistTarget);

			for (int i = 0; i < conForceCount; i++)
			{
				if (owner)
				{
					if (aimAssistTarget[i].gameObject != owner.gameObject && aimAssistTarget[i].CompareTag("Player"))
					{
						rb2d.AddForce((aimAssistTarget[i].transform.position - centerPosition).normalized * constForce, ForceMode2D.Impulse);
						rb2d.velocity = rb2d.velocity.normalized * pushSpeed;
					}
				}
				else
				{
					if (aimAssistTarget[i].CompareTag("Player"))
					{
						rb2d.AddForce((aimAssistTarget[i].transform.position - centerPosition).normalized * constForce, ForceMode2D.Impulse);
						rb2d.velocity = rb2d.velocity.normalized * pushSpeed;
					}
				}
			}
		}

		if (currentState == state.SCRIPTMOVE)
		{
			rb2d.velocity = Vector2.zero;
			//gameObject.layer = noColLayer;
		}

		if (timePushed != 0 && timePushed + destroyTime < Time.time)
		{
			Destroy(gameObject);
		}
	}

	public void ChargeBlock()
	{
		superCharged = true;

		GameObject fireParticles = Instantiate(superChargeFire, transform) as GameObject;
		fireParticles.transform.localPosition = isBig ? new Vector3(1, 1, 0) : new Vector3(0.5f, 0.5f, 0);
	}

	private void reduceColliderSize() {
		c2d.size = c2d.size * 0.9f;
	}

	public void getPushed(Vector2 direction, KinematicPlayer script) {
		currentState = state.PUSHED;

		// reduce collider size, so that adjacent tiles aren't accidently hit
		reduceColliderSize();

		// check if we're inside a block. if we are, destroy ourselves
		RaycastHit2D[] results = new RaycastHit2D[8];
		if (c2d.Cast(Vector2.one, rockFilter, results, 0) > 0) HitDestroy();

		gameObject.layer = projectileLayer;
		rb2d.bodyType = RigidbodyType2D.Dynamic;
		rb2d.velocity = pushSpeed * direction;

		if (!owner) owner = script;

		c2d.isTrigger = false;
		timePushed = Time.time;

		if (attachedObject) attachedObject.DestoryObject();
	}

	public bool getGrabbed(KinematicPlayer script) {
		if( currentState != state.FIXED)
            return false;
		gameObject.layer = noColLayer;
		currentState = state.HELD;
		rb2d.bodyType = RigidbodyType2D.Dynamic;

		if (!owner) owner = script;

		c2d.isTrigger = true;

		if (attachedObject) attachedObject.DestoryObject();
		return true;
	}

	public Vector2 getTop() {
		return new Vector2(transform.position.x + c2d.offset.x, transform.position.y + 0.1f);
	}

	public void HitDestroy()
	{
		Instantiate(destroyParticles, transform.position, destroyParticles.transform.rotation);
		AudioSource.PlayClipAtPoint(destroySound, transform.position, 10f);
		Destroy(gameObject);
	}

	public void JumpDestroy(Vector2 direction)
	{
		Quaternion rotation = Quaternion.LookRotation(Vector3.forward, direction);;
		Instantiate(jumpDestroyParticles, transform.position, rotation);
		Destroy(gameObject);
	}

	IEnumerator DestroyNextFrame() {
		yield return new WaitForEndOfFrame();
		Destroy(gameObject);
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		if (currentState != state.PUSHED) {
			return;
		}
		
		if (other.gameObject.CompareTag("Player")) {
			FindObjectOfType<GameController>().freezeFrame();
			other.gameObject.GetComponent<KinematicPlayer>().GetHit(other.relativeVelocity, superCharged);
		}

		if (other.gameObject.CompareTag("Rock") && destroyOther) { 
			if (other.gameObject.GetComponent<RockScript>().attachedObject) other.gameObject.GetComponent<RockScript>().attachedObject.DestoryObject();
			Destroy(other.gameObject);
		}

		// Get destroyed
		HitDestroy();
	}

	/*
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (currentState != state.PUSHED)
		{
			return;
		}

		if (collision.gameObject.CompareTag("Player"))
		{
			FindObjectOfType<GameController>().freezeFrame();
			collision.gameObject.GetComponent<KinematicPlayer>().GetHit(-rb2d.velocity);
		}

		HitDestroy();
	}
	*/
}
