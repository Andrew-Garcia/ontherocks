using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicPlayer : MonoBehaviour 
{
	bool grounded;
	bool lastGrounded;

	bool doubleJumped;
	private float lastJumpTime = 0;
	private float coyoteTime = 0;
	private float selectedRockTime = 0;

	bool onWall;

	bool stunned = false;

	Rigidbody2D rb2d;
	Vector2 velocity;

	RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
	List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>(16);
	ContactFilter2D contactFilter;

	Animator anim;
	SpriteRenderer sr;

	private bool facingLeft_ = false;
	private float grabbedRocksPositionX;
	private float wallCheckX;

	private RockScript grabbedRock = null;

	Vector2 aimingDirection;
	RockScript selectedRock = null;
	bool shouldGrab = false;

	[Header("Player settings")]
	public int playerNumber = 1;
	[SerializeField] AudioClip deathClip;

	[Header("Combat settings")]
	public float stunTime;
	public float selectedRockTimer = 0.5f;

	[Header("Movement settings")]
	public float speed = 5f;
	public float jumpForce = 5f;
	public float gravityModifier = 1f;
	public float shellRadius = 0.01f;
	public float jumpTimer = 0.15f;
	public float coyoteTimer = 0.1f;

	[Header("Rock Jump")]
	public int rockJumpFrames = 10;
	public float rockJumpDistance = 5f;

	[Header("References")]
	public LayerMask groundLayer;
	public Transform wallChecker;

	public Transform rayOrigin;
	public Transform grabbedRocksPosition;

	GameController gc;

	bool onlyOnce;

    //Animation Variables
    bool isPunching;
    bool lastFacingLeft;

	private bool facingLeft
	{
		get
		{
			return facingLeft_;
		}
		set
		{
			facingLeft_ = value;
			sr.flipX = onWall && !grounded ? !value : value;
			wallChecker.localPosition = new Vector3(
				wallCheckX * (value ? 1 : -1), 
				wallChecker.localPosition.y, 
				wallChecker.localPosition.z
			);

			grabbedRocksPosition.localPosition = new Vector2(
				grabbedRocksPositionX * (value ? -1 : 1),
				grabbedRocksPosition.localPosition.y
			);
		}
	}

	void Start () 
	{
		rb2d = GetComponent<Rigidbody2D>();
		anim = GetComponent<Animator>();
		sr = GetComponent<SpriteRenderer>();

		gc = FindObjectOfType<GameController>();

		grabbedRocksPositionX = grabbedRocksPosition.localPosition.x;
		wallCheckX = wallChecker.localPosition.x;

		getAimingDirection();

		contactFilter.useTriggers = false;
		contactFilter.useLayerMask = true;
		contactFilter.layerMask = ~LayerMask.GetMask("PlayerLayer");

        lastFacingLeft = facingLeft_;
    }

	string getPlayerKey(string keyName)
	{
		return string.Format("Player{0}{1}", playerNumber, keyName);
	}

	private void Update()
	{
		if (Input.GetButtonDown(getPlayerKey("Punch")) && Input.GetButtonDown(getPlayerKey("RockMod"))) Debug.Log("same frame");

		bool nowFacingLeft = facingLeft_;

		// set horizontal velocity
        if (!stunned) velocity.x = Input.GetAxisRaw(getPlayerKey("Horizontal")) * speed;

		//Debug.Log(Input.GetAxisRaw(getPlayerKey("Horizontal")));

		// jump and double jump input
		if ((((grounded || !doubleJumped) && lastJumpTime + jumpTimer < Time.time) 
			|| coyoteTime + coyoteTimer > Time.time) 
			&& Input.GetButtonDown(getPlayerKey("Jump")))
		{
            if (coyoteTime + 0.1f > Time.time) doubleJumped = false;
            else
            {
                doubleJumped = !grounded;
                if (nowFacingLeft != lastFacingLeft)
                {
                    anim.SetBool("IsBackflipping", true);
                }
            }
            lastFacingLeft = nowFacingLeft;
            velocity.y = jumpForce;
			lastJumpTime = Time.time;
		}
		else if (Input.GetButtonUp(getPlayerKey("Jump")))
		{
			//Debug.Log("jump up");
			if (velocity.y > 0) velocity.y = velocity.y * 0.5f;
		}
		else
        {
            anim.SetBool("IsBackflipping", false);
        }

		// punch input
        if (Input.GetButtonDown(getPlayerKey("Punch")))
		{
            if (shouldGrab && !grabbedRock)
                Grab();
            else
            {
                Punch();
            }
        }
        else
        {
            anim.SetBool("IsPunching", false);
        }

		// rock jump input
		if (Input.GetButtonDown(getPlayerKey("Jump")) && Input.GetButton(getPlayerKey("RockMod")))
		{
			if (grabbedRock) StartCoroutine(RockJump());
			else Debug.Log("no rock");
		}

		if (Input.GetButton(getPlayerKey("RockMod")) && grounded)
		{
			velocity = Vector2.zero;
		}

        if (grounded) anim.SetFloat("Velocity", Mathf.Abs(velocity.x));
	}

	void FixedUpdate () 
	{
		velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
		Vector2 deltaPosition = velocity * Time.deltaTime;

		grounded = false;

		Vector2 move = Vector2.up * deltaPosition;
		Move(move, true);  // vertical movement
		move = Vector2.right * deltaPosition;
		Move(move, false); // horizontal movement

		onWall = Physics2D.OverlapCircle(wallChecker.position, 0.25f, groundLayer);

		if (onWall)
		{
			anim.SetBool("OnWall", true);
			doubleJumped = false;
		}
		else anim.SetBool("OnWall", false);

		if (lastGrounded != grounded)
		{
			anim.SetBool("Jump", lastGrounded);
			doubleJumped = false;

			if (lastGrounded && !grounded) coyoteTime = Time.time;
			if (!lastGrounded && grounded) anim.SetTrigger("Landing");
		}
		lastGrounded = grounded;

		getAimingDirection();
		HighlightSelectedRock();
	}

	void Move(Vector2 move, bool yMove)
	{
		float distance = move.magnitude;

		if (distance > 0.001f)
		{
			int count = rb2d.Cast(move, contactFilter, hitBuffer, distance + shellRadius);

			hitBufferList.Clear();
			for (int i = 0; i < count; i++)
			{
				hitBufferList.Add(hitBuffer[i]);
			}

			// bounce when stunned
			if (stunned && hitBufferList.Count > 0) 
			{
				//Debug.Log(velocity.magnitude);
				if (yMove) velocity.y = Vector2.Reflect(velocity, hitBuffer[0].normal).y;
				else velocity.x = Vector2.Reflect(velocity, hitBufferList[0].normal).x;
			}

			for (int i = 0; i < hitBufferList.Count; i++)
			{
				if (!stunned)
				{
					if (yMove)
					{
						if (hitBufferList[i].normal.y > 0.9f)
						{
							grounded = true;
						}

						velocity.y = 0;
					}
					else
					{
						if (grounded)
						{
							// position to step up to
							Vector2 direction = new Vector2(-hitBuffer[i].normal.x * 0.45f, 1f);

							RaycastHit2D[] results = new RaycastHit2D[16];
							ContactFilter2D cf = new ContactFilter2D();

							cf.useLayerMask = true;
							cf.layerMask = ~LayerMask.GetMask("PlayerLayer");

							// BUG HERE - might have to change to some rb2d cast, cause you can step up into blocks.
							// if we run into a block on the side and there is an empty space above it...
							int stepUpColliders = Physics2D.Raycast(transform.position + new Vector3(Mathf.Sign(-hitBuffer[i].normal.x) * 0.5f, 0.5f), 
								Vector2.right * -hitBuffer[i].normal.x, cf, results, 0.9f);

							// step up
							if (stepUpColliders == 0) rb2d.position = rb2d.position + direction;
						}
					}
				}

				float modifiedDistance = hitBufferList[i].distance - shellRadius;
				distance = modifiedDistance < distance ? modifiedDistance : distance;
			}
		}

		rb2d.position = rb2d.position + move.normalized * distance;
	}

	bool Grab()
	{
		RockScript rockScript = selectedRock;
		if (rockScript && rockScript.getGrabbed(this))
		{
			grabbedRock = rockScript;
			return true;
		}

		return false;
	}

	Vector2 getAimingDirection()
	{
		Vector2 newDirection = new Vector2(Input.GetAxisRaw(getPlayerKey("Horizontal")),
										Input.GetAxisRaw(getPlayerKey("Vertical")));
		if (newDirection.magnitude > 0.2f)
		{
			aimingDirection = newDirection.normalized;
			if (newDirection.x != 0) facingLeft = newDirection.x < 0;
		}
		else
		{
			aimingDirection = new Vector2(facingLeft ? -1 : 1, 0);
		}

		return aimingDirection;
	}

	void HighlightSelectedRock()
	{
		if (grabbedRock)
		{
			setSelectedRock(null);
			return;
		}

		RockScript script = null;
		RaycastHit2D frontRayHit = Physics2D.Raycast(rayOrigin.position, aimingDirection, 2f, groundLayer);
		if (frontRayHit)
		{
			script = frontRayHit.collider.GetComponent<RockScript>();
			if (script)
			{
				setSelectedRock(script);
				shouldGrab = false;
				return;
			}
		}
		else
		{
			frontRayHit = Physics2D.Raycast(rayOrigin.position - Vector3.up, aimingDirection, 2f, groundLayer);
			if (frontRayHit)
			{
				script = frontRayHit.collider.GetComponent<RockScript>();
				if (script)
				{
					setSelectedRock(script);
					shouldGrab = false;
					return;
				}
			}
		}

		RaycastHit2D backRayHit = Physics2D.Raycast(rayOrigin.position, -aimingDirection, 10f, groundLayer);
		if (backRayHit)
		{
			script = backRayHit.collider.GetComponent<RockScript>();
			if (script)
			{
				setSelectedRock(script);
				shouldGrab = true;
				return;
			}
		}
		else
		{	
			backRayHit = Physics2D.Raycast(rayOrigin.position - Vector3.up, -aimingDirection, 10f, groundLayer);
			if (backRayHit)
			{
				script = backRayHit.collider.GetComponent<RockScript>();
				if (script)
				{
					setSelectedRock(script);
					shouldGrab = true;
					return;
				}
			}
		}

		if (selectedRock)
		{
			if (onlyOnce)
			{
				selectedRockTime = Time.time;
				onlyOnce = false;
			}

			if (selectedRockTime + selectedRockTimer > Time.time)
			{
				setSelectedRock(selectedRock);
			}
			else setSelectedRock(null);

			return;
		}

		setSelectedRock(null);
	}

	void setSelectedRock(RockScript rock)
	{
		if (rock == selectedRock) return;
		if (selectedRock)
			selectedRock.highlighted = 0;
		if (rock)
		{
			selectedRock = rock;
			selectedRock.highlighted = playerNumber;
			onlyOnce = true;
		}
		else selectedRock = null;
	}

	bool Punch()
	{
		RockScript rockScript;
		if (grabbedRock)
		{
			rockScript = grabbedRock;
			grabbedRock = null;
		}
		else
		{
			rockScript = selectedRock;

			if (rockScript)
			{
				Vector2 rockPos = new Vector2(rockScript.transform.position.x, rockScript.transform.position.y);
				
				if (Physics2D.Raycast(rockPos + rockScript.c2d.offset, getAimingDirection(), rockScript.isBig ? 2f : 1f, groundLayer))
				{
					rockScript.destroyOther = false;
				}
			}

			setSelectedRock(null);
		}
		if (!rockScript) return false;

		Physics2D.IgnoreCollision(GetComponent<Collider2D>(), rockScript.c2d);

		rockScript.getPushed(getAimingDirection());

        bool aimingUp = false;
        bool aimingDown = false;
        bool aimingSide = false;
        //Debug.Log("punchIsRunning");
        anim.SetBool("IsPunching", true);
        if (aimingDirection.y > 0.5f)
        {
            aimingUp = true;
        }
        else
        {
            aimingUp = false;
        }
        if (aimingDirection.y < -0.5f)
        {
            aimingDown = true;
        }
        else
        {
            aimingDown = false;
        }
        if (aimingDirection.x > 0.5f || aimingDirection.x < -0.5f)
        {
            aimingSide = true;
        }
        else
        {
            aimingSide = false;
        }

        if (grounded)
        {
            anim.SetBool("PunchUp", aimingUp && !aimingSide);
            anim.SetBool("PunchDiagonal", aimingUp && aimingSide);
            anim.SetBool("PunchHorizontal", !aimingUp);
        }
        else
        {
            anim.SetBool("AirPunchUp", aimingUp && !aimingSide);
            anim.SetBool("AirPunchDiagonalUp", aimingUp && aimingSide);
            anim.SetBool("AirPunchDiagonalDown", aimingDown && aimingSide);
            anim.SetBool("AirPunchDown", aimingDown && !aimingSide);
            anim.SetBool("AirPunchHorizontal", !aimingDown && !aimingUp);
        }

        //if (grounded) anim.SetTrigger("Punch");
        //else anim.SetTrigger("Punch_air");
        return true;
	}

	IEnumerator RockJump()
	{
		Vector3 direction = Vector3.up;

		int i = 0;
		while (i < rockJumpFrames)
		{
			// freeze position ?
			velocity = Vector2.zero;

			direction = getRockJumpDirection();
			i++;
			yield return null;
		}

		ContactFilter2D cf = new ContactFilter2D();

		cf.useLayerMask = true;
		cf.layerMask = ~LayerMask.GetMask("NoColLayer");

		RaycastHit2D[] hitArray = new RaycastHit2D[16];
		int hitCount = rb2d.Cast(direction, cf, hitArray);

		float distance = rockJumpDistance;
		for (int j = 0; j < hitCount; j++)
		{
			float modifiedDistance = hitArray[j].distance;
			distance = modifiedDistance < distance ? modifiedDistance : distance;
		}

		// put function in rockscript to destroy and play particles
		Destroy(grabbedRock.gameObject);
		rb2d.position = rb2d.position + (Vector2)(distance * direction);

	}

	Vector3 getRockJumpDirection()
	{
		Vector2 newDirection = new Vector2(Input.GetAxisRaw(getPlayerKey("Horizontal")),
											Input.GetAxisRaw(getPlayerKey("Vertical")));
		if (newDirection.magnitude > 0.2f)
		{
			aimingDirection = newDirection.normalized;
		}
		else 
		{
			aimingDirection = Vector3.up;
		}

		return aimingDirection;
	}

	public void GetHit(Vector2 direction)
	{
		StartCoroutine(Stun(-direction));
	}

	IEnumerator Stun(Vector2 direction)
	{
		//Debug.DrawRay(transform.position, direction.normalized * 5f, Color.red, 3f);
		stunned = true;
        anim.SetBool("IsStunned", true);
		velocity = direction * 0.3f;

		yield return new WaitForSeconds(stunTime);

		stunned = false;
        anim.SetBool("IsStunned", false);
    }

	public void PlayerDie()
	{
		gc.onPlayerDie(playerNumber);
		if (selectedRock) selectedRock.highlighted = 0;
		Camera.main.GetComponent<CameraFocus>().RemoveFocus(transform);
		AudioSource.PlayClipAtPoint(deathClip, transform.position, 10f);
		Destroy(gameObject);
	}

	/// <summary>
	/// Sent when an incoming collider makes contact with this object's
	/// collider (2D physics only).
	/// </summary>
	/// <param name="other">The Collision2D data associated with this collision.</param>

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.CompareTag("DeathCollider"))
		{
			if (gameObject)
			{
				PlayerDie();
			}
		}
	}

	/*
	void OnCollisionEnter2D(Collision2D other)
	{
		Debug.Log(other.gameObject + " " + gameObject.name);
		if (other.gameObject.CompareTag("DeathCollider"))
		{
			FindObjectOfType<GameController>().onPlayerDie(playerNumber);
			if (gameObject)
			{
				Camera.main.GetComponent<Camera2D>().RemoveFocus(this.GetComponent<GameEye2D.Focus.F_Transform>());
				AudioSource.PlayClipAtPoint(deathClip, transform.position, 10f);
				Destroy(gameObject);
			}
			return;
		}
		RockScript rockScript = other.gameObject.GetComponent<RockScript>();
		if (!rockScript || rockScript.currentState != RockScript.state.PUSHED)
			return;
		Debug.Log(other.rigidbody.velocity);
		if (!stunned)
		{
			stunned = true;
			StartCoroutine("Stun");
		}
	}
	*/
}
