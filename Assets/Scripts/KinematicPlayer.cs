using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicPlayer : MonoBehaviour 
{
	public enum PlayerState
	{
		MOVE,
		ROCKJUMP,
		ROCKGLIDE,
		BLOCK,
		STANDARDSTUN,
		BLOCKSTUN,
		SUPERPUNCHSTUN,
		TAUNT1,
		TAUNT2,
	}

	bool grounded;
	bool lastGrounded;

	// variables for jumping
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

	// collision velocity calculated in Move() to be used in OnTriggerEnter()
	Vector2 collisionVelocity;

	// used in Move() to register collisions
	RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
	List<RaycastHit2D> hitBufferList = new List<RaycastHit2D>(16);

	Animator anim;
	SpriteRenderer sr;

	private bool facingLeft_ = false;
	private float grabbedRocksPositionX;
	private float wallCheckX;

	// current grabbed rock
	private RockScript grabbedRock = null;

	Vector2 aimingDirection;
	RockScript selectedRock = null;
	bool shouldGrab = false;

	[Header("Player settings")]
	public bool freezeOnRockJump = true;
	public int playerNumber = 1;
	public float lavaBounceVelocity = 7f;
	[SerializeField] AudioClip deathClip;

	[Header("Combat settings")]
	public float stunTime;
	public float selectedRockTimer = 0.5f;
	public float damage = 0;

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

	[Header("Rock Glide")]
	public float rockGlideFallSpeed = 8;

	[Header("Block")]
	public int blockFrames = 15;
	int blockNumber = 0;

	// used to store if the next/current rock should be charged
	// 3 options on when the block is charged:
	//   > we're already holding a block, done in GetHit()
	//   > the next block we pick up, done in Grab()
	//   > the next block we punch, done in Punch()
	bool superPunchChargeTrigger = false;
	bool hasSuperPunch = false;

	[Header("References")]
	public Transform wallChecker;
	public Transform rayOrigin;
	public Transform grabbedRocksPosition;

	LayerMask groundLayer;

	[Header("Particles")]
    public ParticleSystem dashDust;
	public ParticleSystem landingDust;
	public ParticleSystem superPunchFire;
	public GameObject incenseSmoke;

	// used for storing the current taunt smoke for stopping it
	GameObject currentIncenseSmoke;

	GameController gc;

	bool onlyOnce;

	ContactFilter2D rockContactFilter;

    // Animation Variables
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
					currentState = PlayerState.BLOCK;
					StartCoroutine(Block());
				}

				// rock glide input
				if (Input.GetButtonDown(getPlayerKey("RockMod")) && !grounded && grabbedRock)
				{
					currentState = PlayerState.ROCKGLIDE;
					StartCoroutine(RockGlide());
				}

				// taunt input
				if (Input.GetButtonDown(getPlayerKey("Taunt1")) && grounded)
				{
					currentState = PlayerState.TAUNT1;
					anim.SetTrigger("taunt1");
				}

				if (Input.GetButtonDown(getPlayerKey("Taunt2")) && grounded)
				{
					currentState = PlayerState.TAUNT2;
					anim.SetBool("taunt2", true);
					anim.SetTrigger("taunt2trigger");
				}

                //Pause button (+ or -); Loads level select menu
                if (Input.GetButtonDown(getPlayerKey("Pause")))
                {
                    FindObjectOfType<LevelSelector>().LoadMenu();
                }

				break;

			case PlayerState.ROCKJUMP:

				break;

			case PlayerState.ROCKGLIDE:
				velocity.x += Input.GetAxisRaw(getPlayerKey("Horizontal")) * speed / 10;

				if (Input.GetAxisRaw(getPlayerKey("Horizontal")) == 0) velocity.x = 0;

				if (Mathf.Abs(velocity.x) > speed) velocity.x = Mathf.Sign(velocity.x) * speed;
				if (velocity.y <= 0) velocity.y = -rockGlideFallSpeed;

				if (Input.GetButtonDown(getPlayerKey("Jump")))
				{
					StartCoroutine(RockJump());
				}

				if (grounded || Input.GetButtonUp(getPlayerKey("RockMod")))
				{
					currentState = PlayerState.MOVE;
					grabbedRock.currentState = RockScript.state.HELD;
				}

				break;

			case PlayerState.STANDARDSTUN:
				break;

			case PlayerState.BLOCK:
				if (grounded) velocity.x = 0;
				break;

			case PlayerState.TAUNT1:
				velocity.x = 0;
				break;

			case PlayerState.TAUNT2:
				velocity.x = 0;
				if (Input.GetButtonUp(getPlayerKey("Taunt2")))
				{
					if (currentIncenseSmoke)
					{
						var main = currentIncenseSmoke.GetComponent<ParticleSystem>().main;
						main.loop = false;
						currentIncenseSmoke = null;
					}
					anim.SetBool("taunt2", false);
				}

				break;
		}

        if (grounded) anim.SetFloat("Velocity", Mathf.Abs(velocity.x));
		
	}

	void FixedUpdate()
	{
		// set y velocity in fixed update so it's synced with Move()
		if (freezeOnRockJump)
		{
			if (currentState != PlayerState.ROCKJUMP && (currentState != PlayerState.ROCKGLIDE || velocity.y > 0)) velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
		}
		else {
			if (currentState != PlayerState.ROCKGLIDE || velocity.y > 0) velocity += gravityModifier * Physics2D.gravity * Time.deltaTime;
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

			// in standard stun: when you hit a rock at a certain velocity, bounce off
			if (currentState == PlayerState.STANDARDSTUN && hitBufferList.Count > 0) 
			{
				if (yMove && Mathf.Abs(velocity.y) > 10)
				{
					velocity.y = Vector2.Reflect(velocity, hitBuffer[0].normal).y;
				}
				else if (!yMove && Mathf.Abs(velocity.x) > 10)
				{
					velocity.x = Vector2.Reflect(velocity, hitBufferList[0].normal).x;
				}

				velocity /= 1.025f;
			}

			// in superpunch stun: when you hit a rock, slow down
			if (currentState == PlayerState.SUPERPUNCHSTUN && hitBufferList.Count > 0)
			{
				if (yMove && Mathf.Abs(velocity.y) > 10)
				{
					for (int i = 0; i < hitBufferList.Count; i++)
					{
						hitBufferList[i].transform.GetComponent<RockScript>().HitDestroy();
					}

					velocity /= 1.25f;
				}
				else if (!yMove && Mathf.Abs(velocity.x) > 10)
				{
					for (int i = 0; i < hitBufferList.Count; i++)
					{
						hitBufferList[i].transform.GetComponent<RockScript>().HitDestroy();
					}

					velocity /= 1.25f;
				}
			}

			for (int i = 0; i < hitBufferList.Count; i++)
			{
				if (yMove)
				{
					if (hitBufferList[i].normal.y > 0.9f)
					{
						grounded = true;
					}

					// reset vertical velocity unless we're stunned, then allow bouncing/colliding vertically above a certain threshold
					if ((currentState != PlayerState.SUPERPUNCHSTUN && currentState != PlayerState.STANDARDSTUN) || Mathf.Abs(velocity.y) < 10) velocity.y = 0;
				}
				else
				{
					if (grounded && currentState != PlayerState.STANDARDSTUN && currentState != PlayerState.SUPERPUNCHSTUN)
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
			// if we have supercharge, charge the next block we grab
			if (superPunchChargeTrigger)
			{
				rockScript.ChargeBlock();
				superPunchChargeTrigger = false;
			}

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
			// use the rock that's selected and punch it (should be the one in front of you)
			rockScript = selectedRock;

			if (rockScript)
			{
				// if we have superpunch, charge the block we're punching
				if (superPunchChargeTrigger)
				{
					rockScript.ChargeBlock();
					superPunchChargeTrigger = false;
				}

				// if there's a rock in front of the selected rock, don't destroy it when punched (to simulate digging)
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

		// lose superpunch when you punch a block
		if (hasSuperPunch)
		{
			superPunchFire.Stop();
			hasSuperPunch = false;
		}

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

	// blocks counted in GetHit()
	IEnumerator Block()
	{
		anim.SetBool("Block", true);

		for (int i = 0; i < blockFrames; i++)
		{
			yield return null;
		}

		currentState = PlayerState.MOVE;
		anim.SetBool("Block", false);
	}

	// rock jump function
	IEnumerator RockJump()
	{
		currentState = PlayerState.ROCKJUMP;
        // resets and plays the dashdust particle
        dashDust.time = 0;
        dashDust.Play();

        Vector3 direction = Vector3.up;

		int i = 0;

		// set rock to state with no physics velocity
		grabbedRock.currentState = RockScript.state.SCRIPTMOVE;

		Vector3 rockEnd = transform.position - (grabbedRock.isBig ? new Vector3(1f, 0.75f) 
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

	IEnumerator RockGlide()
	{
		// set current rock to state with no physics
		grabbedRock.currentState = RockScript.state.SCRIPTMOVE;

		Vector3 offset = (grabbedRock.isBig ? new Vector3(1f, 0.75f)
				: new Vector3(0.5f, 0.875f));

		Vector3 rockEnd;

		//Vector3 rockStart = grabbedRock.transform.position;

		float i = 0;
		// bring rock to under our feet
		while (i < 1)
		{
			i += Time.deltaTime / 0.2f;

			rockEnd = transform.position - offset;

			grabbedRock.transform.position = Vector3.Lerp(grabbedRock.transform.position, rockEnd, i);

			yield return null;
		}

		while (currentState == PlayerState.ROCKGLIDE)
		{
			grabbedRock.transform.position = transform.position - offset;
			yield return null;
		}
	}

	Vector3 getRockJumpDirection()
	{
		Vector2 newDirection = new Vector2(Input.GetAxisRaw(getPlayerKey("Horizontal")),
											Input.GetAxisRaw(getPlayerKey("Vertical")));

		if (newDirection.magnitude > 0.2f) aimingDirection = newDirection.normalized;
		else aimingDirection = Vector3.up;

		return aimingDirection;
	}

	public void GetHit(Vector2 direction, bool superCharged)
	{
		if (currentState == PlayerState.BLOCK) 
		{
			velocity = direction * 0.3f;
			if (!hasSuperPunch) blockNumber++;

			// on third block, gain supercharge
			if (blockNumber == 3)
			{
				GainSuperCharge();
				blockNumber = 0;
			}

			return;
		}
		StartCoroutine(Stun(-direction, superCharged));
	}

	void GainSuperCharge()
	{
		if (hasSuperPunch) return;

		hasSuperPunch = true;

		superPunchFire.Play();
		superPunchChargeTrigger = true;

		// if we're holding a rock, set it to be super charged
		if (grabbedRock)
		{
			grabbedRock.ChargeBlock();
			superPunchChargeTrigger = false;
		}
	}

	IEnumerator Stun(Vector2 direction, bool superPunchStun)
	{
		currentState = superPunchStun ? PlayerState.SUPERPUNCHSTUN : PlayerState.STANDARDSTUN;

		// this is so a super pickup doesn't spawn when you crash through blocks, since it's easy to chain pick ups together otherwise
		bool resetPickUp = true;
		if (gc.superPickUpActive) resetPickUp = false;
		else gc.superPickUpActive = true;

        anim.SetBool("IsStunned", true);
		velocity = direction * 0.3f;

		yield return new WaitForSeconds(stunTime);

		currentState = PlayerState.MOVE;
        anim.SetBool("IsStunned", false);

		if (resetPickUp) gc.superPickUpActive = false;
    }

	public void PlayerDie()
	{
		// lose superpunch when you die
		if (superPunchFire.isPlaying) superPunchFire.Stop();
		if (selectedRock) selectedRock.highlighted = 0;

		gc.onPlayerDie(playerNumber);
		Camera.main.GetComponent<CameraFocus>().RemoveFocus(transform);
		AudioSource.PlayClipAtPoint(deathClip, transform.position, 10f);
		Destroy(gameObject);
	}

	// run function in animation to spawn incense particles
	public void LightIncenseTaunt()
	{
		currentIncenseSmoke = Instantiate(incenseSmoke, transform.position + new Vector3(facingLeft ? -0.9f : 0.72f, -0.43f, 0), Quaternion.identity);
	}

	// run function in the animation to change back to move state
	public void EndTaunt()
	{
		currentState = PlayerState.MOVE;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.CompareTag("DeathCollider"))
		{
			if (gameObject)
			{
				// death on entering the DeathCollider
				PlayerDie();
			}
		}

		Transform parent = collision.gameObject.transform.parent;

		if (parent)
		{
			if (parent.CompareTag("Lava"))
			{
				// take damage and throw player based on health/percent
				velocity.y = lavaBounceVelocity;
			}
		}

		if (collision.CompareTag("SuperPickUp"))
		{
			GainSuperCharge();
			gc.superPickUpActive = false;
			Destroy(collision.gameObject);
		}
	}
}
