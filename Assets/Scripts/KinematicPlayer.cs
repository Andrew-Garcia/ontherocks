using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicPlayer : MonoBehaviour 
{
	public enum PlayerState
	{
		MOVE,
		ROCKJUMP,
		BLOCK,
		STANDARDSTUN,
		BLOCKSTUN,
		SUPERPUNCHSTUN,
	}

	bool grounded;
	bool lastGrounded;

	bool doubleJumped;
	private float lastJumpTime = 0;
	private float coyoteTime = 0;
	private float selectedRockTime = 0;
	private float backFlipTime = 0;

	bool onWall;

	bool stunned = false;
	bool freezePosition = false;

	Rigidbody2D rb2d;
	Vector2 velocity;
	BoxCollider2D col;

	RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
	List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>(16);

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
	public bool freezeOnRockJump = true;
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
	float backFlipTimer = 0.1f;

	[Header("Rock Jump")]
	public int preRockJumpFrames = 10;
	public int rockJumpFrames = 10;
	public float rockJumpSpeed = 5f;

	[Header("Block")]
	public int blockFrames = 15;
	int blockNumber = 0;

	[Header("References")]
	public Transform wallChecker;

	LayerMask groundLayer;

	public Transform rayOrigin;
	public Transform grabbedRocksPosition;
    public ParticleSystem dashDust;
	public ParticleSystem landingDust;

	GameController gc;

	bool onlyOnce;

	ContactFilter2D rockContactFilter;

    //Animation Variables
    bool isPunching;
    bool lastFacingLeft;

	public PlayerState currentState = PlayerState.MOVE;

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
		col = GetComponent<BoxCollider2D>();

		gc = FindObjectOfType<GameController>();

		grabbedRocksPositionX = grabbedRocksPosition.localPosition.x;
		wallCheckX = wallChecker.localPosition.x;

		getAimingDirection();

		groundLayer = LayerMask.GetMask("Ground");

		rockContactFilter.useTriggers = false;
		rockContactFilter.useLayerMask = true;
		rockContactFilter.layerMask = ~(LayerMask.GetMask("PlayerLayer", "NoColLayer"));

		lastFacingLeft = facingLeft_;

		blockNumber = 0;
    }

	string getPlayerKey(string keyName)
	{
		return string.Format("Player{0}{1}", playerNumber, keyName);
	}

	private void Update()
	{
		bool nowFacingLeft = facingLeft_;

		switch (currentState) 
		{
			case PlayerState.MOVE:
				// set horizontal velocity
				velocity.x = Input.GetAxisRaw(getPlayerKey("Horizontal")) * speed;

				// jump and double jump input
				if ((((grounded || !doubleJumped) && lastJumpTime + jumpTimer < Time.time)	
					|| coyoteTime + coyoteTimer > Time.time)
					&& Input.GetButtonDown(getPlayerKey("Jump"))
					&& !Input.GetButton(getPlayerKey("RockMod")))
				{

					// if you press jump while the coyote timer is active, you haven't double jumped, otherwise, set the value normally
					if (coyoteTime + 0.1f > Time.time)
						doubleJumped = false;
					else
						doubleJumped = !grounded;
					
					// if we jump in air within the backflip timer, backflip
					if (doubleJumped && backFlipTime + backFlipTimer > Time.time)
						anim.SetBool("IsBackflipping", true);

					velocity.y = jumpForce;
					lastJumpTime = Time.time;
				}
				else if (Input.GetButtonUp(getPlayerKey("Jump")))
				{
					if (velocity.y > 0) velocity.y = velocity.y * 0.5f;
				}
				else
				{
					anim.SetBool("IsBackflipping", false);
				}

				// if we switch directions in air start the backflip timer 
				if (lastFacingLeft != nowFacingLeft && !grounded)
					backFlipTime = Time.time;
				
				lastFacingLeft = nowFacingLeft;

				// punch input
				if (Input.GetButtonDown(getPlayerKey("Punch")))
				{
						if (shouldGrab && !grabbedRock)
						Grab();
					else
						Punch();
				}
				else
				{
					anim.SetBool("IsPunching", false);
				}

				// rock jump input
				if (Input.GetButtonDown(getPlayerKey("Jump")) && Input.GetButton(getPlayerKey("RockMod")))
				{
					if (grabbedRock) StartCoroutine(RockJump());
				}

				// aiming input
				if (Input.GetButton(getPlayerKey("RockMod")) && grounded)
				{
					velocity = Vector2.zero;
				}

				// block input
				if (Input.GetButtonDown(getPlayerKey("Block")))
				{
					StartCoroutine(Block());
				}

				break;

			case PlayerState.ROCKJUMP:
			case PlayerState.STANDARDSTUN:
				
				
				break;

			case PlayerState.BLOCK:
				if (grounded) velocity.x = 0;
				break;
		}

        if (grounded) anim.SetFloat("Velocity", Mathf.Abs(velocity.x));
	}

	void FixedUpdate()
	{
		if (freezeOnRockJump)
		{
			if (currentState != PlayerState.ROCKJUMP) velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
		}
		else {
			velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
		}

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

			// if we've gone from on ground to not on ground, start the coyote timer
			if (lastGrounded && !grounded) coyoteTime = Time.time;

			// if we've gone from not on ground to on ground, do the landing animation
			if (!lastGrounded && grounded)
			{
				anim.SetTrigger("Landing");
				landingDust.Play();
			}
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
			int count = rb2d.Cast(move, rockContactFilter, hitBuffer, distance + shellRadius);

			hitBufferList.Clear();
			for (int i = 0; i < count; i++)
			{
				hitBufferList.Add(hitBuffer[i]);
			}

			// in standard stun: when you hit a rock, bounce off
			if (currentState == PlayerState.STANDARDSTUN && hitBufferList.Count > 0) 
			{
				if (yMove) velocity.y = Vector2.Reflect(velocity, hitBuffer[0].normal).y;
				else velocity.x = Vector2.Reflect(velocity, hitBufferList[0].normal).x;
			}

			// in superpunch stun: when you hit a rock, slow down
			if (currentState == PlayerState.SUPERPUNCHSTUN && hitBufferList.Count > 0)
			{
				//velocity /= 2;
				Debug.Log("oof");
			}

			for (int i = 0; i < hitBufferList.Count; i++)
			{
				if (currentState != PlayerState.STANDARDSTUN && currentState != PlayerState.SUPERPUNCHSTUN)
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
							// position to check block in
							Vector2 direction = new Vector2(-hitBuffer[i].normal.x * 0.9f, 1f);

							// position to step up into
							Vector2 newPos = new Vector2(-hitBuffer[i].normal.x * 0.1f, 1f);

							RaycastHit2D[] results = new RaycastHit2D[16];

							// if we run into a block on the side and there is an empty space above it...
							int stepUpColliders = Physics2D.BoxCast((Vector2)transform.position + direction + col.offset, 
								col.size, 0, Vector2.one, rockContactFilter, results, 0);

							// step up
							if (stepUpColliders == 0)
							{
								rb2d.position = rb2d.position + newPos;
							}
						}
					}
				}

				float modifiedDistance = hitBufferList[i].distance - shellRadius;
				distance = modifiedDistance < distance ? modifiedDistance : distance;
			}
		}

		rb2d.position = rb2d.position + move.normalized * distance;
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

		// surely we can consolidate these 4 raycasts into a function...
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

	bool Punch()
	{
		RockScript rockScript;
		
		// if we're holding a rock, use that rock and empty our grabbed rock spot
		if (grabbedRock)
		{
			rockScript = grabbedRock;
			grabbedRock = null;
		}
		// otherwise...
		else
		{
			// use the rock that's selected and punch it
			rockScript = selectedRock;

			// if there's a rock in front of the selected rock, don't destroy it when punched (to simulate digging)
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

		// if we're not holding a rock or selecting one, return
		if (!rockScript) return false;

		Physics2D.IgnoreCollision(GetComponent<Collider2D>(), rockScript.c2d);

		rockScript.getPushed(getAimingDirection(), this);

		// animation
        bool aimingUp = false;
        bool aimingDown = false;
        bool aimingSide = false;
        
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

	IEnumerator Block()
	{
		anim.SetBool("Block", true);
		currentState = PlayerState.BLOCK;

		for (int i = 0; i < blockFrames; i++)
		{
			yield return null;
		}

		currentState = PlayerState.MOVE;
		anim.SetBool("Block", false);
	}

	IEnumerator RockJump()
	{
		currentState = PlayerState.ROCKJUMP;
        // resets and plays the dashdust particle
        dashDust.time = 0;
        dashDust.Play();

        Vector3 direction = Vector3.up;

		int i = 0;

		// set rock to state with no outsidunitye velocity
		grabbedRock.currentState = RockScript.state.SCRIPTMOVE;

		Vector3 rockEnd = transform.position - (grabbedRock.isBig ? new Vector3(1f, 1.375f) 
			: new Vector3(0.5f, 0.875f));

		Vector3 rockStart = grabbedRock.transform.position;

		// freeze current position, get direction to jump to, bring rock to under our feet
		if (freezeOnRockJump) velocity = Vector2.zero;
		while (i < preRockJumpFrames)
		{
			grabbedRock.transform.position = Vector3.Lerp(rockStart, rockEnd, (float)i * 2/ preRockJumpFrames);
			direction = getRockJumpDirection();
			i++;
			yield return null;
		}

		grabbedRock.JumpDestroy(direction);

		// set velocity to speed and direction of the jump
		velocity = direction * rockJumpSpeed;

		// apply velocity for rockJumpFrames
		for (int j = 0; j < rockJumpFrames; j++)
		{ 
			yield return null;
		}

		// reset velocity after jump
		velocity = Vector2.zero;
		currentState = PlayerState.MOVE;
	}

	Vector3 getRockJumpDirection()
	{
		Vector2 newDirection = new Vector2(Input.GetAxisRaw(getPlayerKey("Horizontal")),
											Input.GetAxisRaw(getPlayerKey("Vertical")));

		if (newDirection.magnitude > 0.2f) aimingDirection = newDirection.normalized;
		else aimingDirection = Vector3.up;

		return aimingDirection;
	}

	public void GetHit(Vector2 direction)
	{
		if (currentState == PlayerState.BLOCK) 
		{
			velocity = direction * 0.3f;
			blockNumber++;
			return;
		}
		StartCoroutine(Stun(-direction));
	}

	IEnumerator Stun(Vector2 direction)
	{
		currentState = PlayerState.STANDARDSTUN;
        anim.SetBool("IsStunned", true);
		velocity = direction * 0.3f;

		yield return new WaitForSeconds(stunTime);

		currentState = PlayerState.MOVE;
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

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.CompareTag("DeathCollider") || collision.gameObject.transform.parent.CompareTag("Lava"))
		{
			if (gameObject)
			{
				PlayerDie();
			}
		}
	}
}
