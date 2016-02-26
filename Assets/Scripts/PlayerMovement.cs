using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This handles movement input- the directional keys, jumping,
/// running, and mouse-look.
/// </summary>
[RequireComponent(typeof(BackgroundSound))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
	public GameObject ourlevel;
	public static int levelNumber;
	public static int maxlevelNumber;
	public static int playerScore;
	public static int usingController;
	public static float guardianHostility;
	public static Vector3 playerPosition = new Vector3 (1610f, 2000f, 2083f);
	public static Quaternion playerRotation = new Quaternion (0f, 0f, 0f, 0f);
	public static float initialTurn = 0f;
	public static float initialUpDown = 0f;
	public Camera mainCamera;
	public Camera wireframeCamera;
	public Camera skyboxCamera;
	public Material overlayBoxMat;
	public Material overlayBoxMat2;
	public GameObject maxbotsText;
	public GameObject countdownText;
	public Text maxbotsTextObj;
	public Text countdownTextObj;
	public static int countdown;
	private int countdownTicker = 30;
	//we're running the physics engine at 30 fps
	public GameObject cameraDolly;
	public ParticleSystem particlesystem;
	public AnimationCurve slopeCurveModifier = new AnimationCurve (new Keyframe (-90.0f, 1.0f), new Keyframe (0.0f, 1.0f), new Keyframe (90.0f, 0.0f));
	public int yourMatch;
	public Color32[] yourBrain;
	public float activityRange = 30f;
	private int brainPointer;
	private float altitude = 1f;
	private Rigidbody rigidBody;
	private Vector3 startPosition;
	private Vector3 endPosition;
	private Vector3 lastPlayerPosition;
	private float stepsBetween;
	private LayerMask onlyTerrains;
	private SphereCollider sphereCollider;
	private AudioSource audiosource;
	public float yourMatchDistance = 9999;
	public bool yourMatchOccluded = true;
	private int pingTimer = 2;
	private BackgroundSound backgroundSound;
	public AudioClip botBeep;
	public float baseFOV = 68f;
	public float mouseSensitivity = 100f;
	public float mouseDrag = 0f;
	public float baseJump;
	public float maximumBank = 1f;
	private Vector3 desiredMove = Vector3.zero;
	private bool releaseJump = true;
	private float moveLimit = 0f;
	private float deltaTime = 0f;
	private float xRot = 0f;
	private float yRot = 0f;
	private float yCorrect = 0f;
	private float zRot = 0f;
	public float clampRotateAngle = Mathf.PI * 2f;
	private float lookAngleUpDown = Mathf.PI * 0.2f;
	public Vector3 desiredAimOffsetPosition;
	public int botNumber;
	public int totalBotNumber;
	private int blurHack;
	private Quaternion blurHackQuaternion;
	private GameObject allbots;
	private GameObject guardian;
	private GuardianMovement guardianmovement;
	public Vector3 locationOfCounterpart;
	private RaycastHit hit;
	private float cameraZoom = 0f;
	public float dollyOffset;
	public float timeBetweenGuardians = 1f;
	public float creepToRange;
	public float creepRotAngle = 1f;
	public float skyboxRot = 0f;
	public float skyboxRot2 = 0f;
	private float velCompensated = 0.00025f;
	private Vector3 positionOffset = new Vector3 (40f, -40f, 0f);
	private GameObject level;
	private SetUpBots setupbots;
	private GameObject packetmeter;
	private RectTransform packetdisplay;
	private GameObject speedmeter;
	private RectTransform speeddisplay;
	public int packetDisplayIncrement = 0;
	public int packets = 1000;
	private int lastpackets = 1;
	public int speed = 0;
	private int lastspeed = 1;
	public Texture2D colorBits;
	public CanvasRenderer colorDisplay;
	private bool notStartedColorBitsScreen;
	public Renderer ourbody;
	private bool supportsRenderTextures;

	WaitForSeconds playerWait = new WaitForSeconds(0.015f);



	void Awake ()
	{
		backgroundSound = GetComponent<BackgroundSound> ();
		rigidBody = GetComponent<Rigidbody> ();
		sphereCollider = GetComponent<SphereCollider> ();
		audiosource = GetComponent<AudioSource> ();
		audiosource.priority = 1;
		//stuff bolted onto the player is always most important
		allbots = GameObject.FindGameObjectWithTag ("AllBots").gameObject;
		guardian = GameObject.FindGameObjectWithTag ("GuardianN").gameObject;
		ourbody = transform.FindChild ("PlayerBody").GetComponent<Renderer> ();

		colorBits = new Texture2D (24, 2, TextureFormat.ARGB32, false);
		colorBits.filterMode = FilterMode.Point;
		for (int i = 0; i < 24; i++) {
			colorBits.SetPixel(i,0,Color.clear);
			colorBits.SetPixel(i,1,Color.clear);
		}
		colorBits.Apply();
		colorDisplay = GameObject.FindGameObjectWithTag ("colorbits").GetComponent <CanvasRenderer>();
		notStartedColorBitsScreen = true;
		//this should be our screen

		supportsRenderTextures = SystemInfo.supportsRenderTextures;

		guardianmovement = guardian.GetComponent<GuardianMovement> ();
		onlyTerrains = 1 << LayerMask.NameToLayer ("Wireframe");
		level = GameObject.FindGameObjectWithTag ("Level");
		packetmeter = GameObject.FindGameObjectWithTag ("packets");
		packetdisplay = packetmeter.GetComponent<RectTransform> ();
		packets = 1000;
		speedmeter = GameObject.FindGameObjectWithTag ("speed");
		speeddisplay = speedmeter.GetComponent<RectTransform> ();
		setupbots = level.GetComponent<SetUpBots> ();
		locationOfCounterpart = Vector3.zero;
		levelNumber = PlayerPrefs.GetInt ("levelNumber", 2);
		maxlevelNumber = PlayerPrefs.GetInt ("maxlevelNumber", 2);
		playerScore = PlayerPrefs.GetInt ("playerScore", 0);
		usingController = PlayerPrefs.GetInt ("usingController", 0);
		guardianHostility = PlayerPrefs.GetFloat ("guardianHostility", 0f);
	}

	void Start () {
		if (Physics.Raycast (playerPosition, Vector3.down, out hit)) playerPosition = hit.point + Vector3.up;
		transform.position = playerPosition;
		startPosition = transform.position;
		endPosition = transform.position;
		lastPlayerPosition = transform.position;
		stepsBetween = 0f;
		baseJump = Mathf.Sqrt (levelNumber) * 0.5f;
		//make it so we can get around in the crazy levels
		blurHack = 0;
		botNumber = levelNumber;
		totalBotNumber = levelNumber;
		dollyOffset = 16f / levelNumber;
		maxbotsTextObj = maxbotsText.GetComponent<Text> ();
		countdownTextObj = countdownText.GetComponent<Text> ();
		//start off with the full amount and no meter updating
		creepToRange = (float)Mathf.Min (1800, levelNumber);
		//somewhat randomized but still in the area of what's set
		creepRotAngle = UnityEngine.Random.Range (0f, 359f);
		guardianmovement.locationTarget = new Vector3 (2000f + (Mathf.Sin (Mathf.PI / 180f * creepRotAngle) * 2000f), 100f, 2000f + (Mathf.Cos (Mathf.PI / 180f * creepRotAngle) * 2000f));
		guardian.transform.position = guardianmovement.locationTarget;
		//set up the scary monster to be faaaar away to start. It will circle.
		maxbotsTextObj.text = string.Format("high score:{0:0.}", playerScore);
		countdown = 20 + (int)(Math.Sqrt(levelNumber)*30f); // scales to size but gets very hard to push. Giving too much time gets us into the 'CPUbound' zone too easy
		countdownTextObj.text = string.Format ("{0:0.}m (+{1:0.})", countdown/60, Mathf.Sqrt(countdown));
		packetdisplay.sizeDelta = new Vector2 (35f, (packets / 1000f) * (Screen.height - 24f));
		colorBits.Apply();
		StartCoroutine ("SlowUpdates");
		//start this only once with a continuous loop inside the coroutine
	}

	void OnApplicationQuit () {
		if (setupbots.gameEnded != true) {
			PlayerPrefs.SetInt ("levelNumber", 2);
			PlayerPrefs.SetInt ("maxlevelNumber", 2);
			PlayerPrefs.SetInt ("playerScore", 0);
			PlayerPrefs.SetFloat ("guardianHostility", 0);
			PlayerPrefs.Save ();
			//if we are quitting, it's like a total reset. Arcade mode.
			//BUT, if we're quitting out of the win screen we can resume.
		}
	}

	void OnParticleCollision (GameObject shotBy)
	{
		if (shotBy.CompareTag("Line")) {
			packets = (int)Mathf.Lerp(packets, 1000f, 0.1f);
			//anytime you get hit with a zap, it powers you up
			//but only if it's BotZaps that fired the zap
			//it's tagged with "Line"
			//it's also more efficient as bots no longer target you for zaps
		}
	}

	void Update ()
	{
		//Each frame we run Update, regardless of what game control/physics is doing. This is the fundamental 'tick' of the game but it's wildly time-variant: it goes as fast as possible.
		cameraDolly.transform.localPosition = Vector3.Lerp(startPosition, endPosition, stepsBetween) + (Vector3.up * dollyOffset);
		stepsBetween += (Time.deltaTime * Time.fixedDeltaTime);

		if (usingController == 0) {
			float tempMouse = Input.GetAxis ("MouseX") / mouseSensitivity;
			mouseDrag = Mathf.Abs (tempMouse);
			initialTurn = Mathf.Lerp(initialTurn, initialTurn - tempMouse, Mathf.Abs(Input.GetAxis ("MouseX")*0.1f));
			tempMouse = Input.GetAxis ("MouseY") / mouseSensitivity;
			if (Mathf.Abs (tempMouse) > mouseDrag) mouseDrag = Mathf.Abs (tempMouse);
			initialUpDown = Mathf.Lerp(initialUpDown, initialUpDown + tempMouse, Mathf.Abs(Input.GetAxis ("MouseY")*0.1f));
			tempMouse = -tempMouse;
			if (initialTurn < 0f)
				initialTurn += clampRotateAngle;
			if (initialTurn > clampRotateAngle)
				initialTurn -= clampRotateAngle;
			initialUpDown = Mathf.Clamp (initialUpDown, -1.5f, 1.5f);
			//mouse is instantaneous so it can be in Update.
		}

		if (supportsRenderTextures) {
			blurHack += 1;
			if (blurHack > 1) blurHack = 0;
			blurHackQuaternion = wireframeCamera.transform.localRotation;
			if (blurHack == 0) {
				blurHackQuaternion.y = velCompensated;
				blurHackQuaternion.x = velCompensated;
				//blurHackQuaternion.z = -velCompensated;
				wireframeCamera.transform.localPosition = positionOffset * -velCompensated;
			}
			if (blurHack == 1) {
				blurHackQuaternion.y = -velCompensated;
				blurHackQuaternion.x = -velCompensated;
				//blurHackQuaternion.z = velCompensated;
				wireframeCamera.transform.localPosition = positionOffset * velCompensated;
			}
			wireframeCamera.transform.localRotation = blurHackQuaternion;
		}

		yRot = Mathf.Sin (initialUpDown);
		yCorrect = Mathf.Cos (initialUpDown);
		xRot = Mathf.Cos (initialTurn) * yCorrect;
		zRot = Mathf.Sin (initialTurn) * yCorrect;
		//there's our angle math

		desiredAimOffsetPosition = transform.localPosition;
		desiredAimOffsetPosition += new Vector3 (xRot, yRot, zRot);
		mainCamera.transform.LookAt (desiredAimOffsetPosition);
		//We simply offset a point from where we are, using simple orbital math, and look at it
		//The positioning is simple and predictable, and LookAt is great at translating that into quaternions.
		
		if ((Input.GetButton("Jump") || Input.GetButton("KeyboardJump")) && releaseJump) {
			if (Physics.Raycast (transform.position, Vector3.down, out hit)){
				rigidBody.AddForce (Vector3.up * baseJump / Mathf.Pow(hit.distance, 3), ForceMode.Impulse);
				releaseJump = false;
				//if you jump you can climb steeper walls, but not vertical ones
				//we can trigger the jump at any time in Update, but only once for each FixedUpdate
				//then we gotta wait for FixedUpdate to space it out again. This will work for any twitch control
				//also, we don't have to care which input system is driving it with this
			}
		}

	}
			
	void FixedUpdate ()
	{
		//FixedUpdate is run as many times as needed, before an Update step: or, it's skipped if framerate is super high.
		//For this reason, if framerate is known to be always higher than 50fps, stuff can be put here to help the engine run faster
		//but if framerate's running low, we are not actually getting a spaced out distribution of frames, only a staggering of them
		//to allow physics to run correctly.
		playerPosition = transform.position;
		playerRotation = transform.rotation;
		dollyOffset -= 0.06f;
		if (dollyOffset < 0f) dollyOffset = 0f;

		countdownTicker -= 1;
		if (countdownTicker < 1) {
			countdownTicker = 30;
			if (setupbots.gameEnded == false) {
				countdown -= 1;
				if (countdown < 0)
					countdownTextObj.text = string.Format ("{0:0.}s (-{1:0.})", countdown, Mathf.Sqrt (-countdown));
				else {
					if (countdown > 60) {
						countdownTextObj.text = string.Format ("{0:0.}m (+{1:0.})", countdown / 60, Mathf.Sqrt (countdown));
					} else {
						countdownTextObj.text = string.Format ("{0:0.}s (+{1:0.})", countdown, Mathf.Sqrt (countdown));
					}
				}
			}
		}
		//the timer section: if we're timed play, we run the countdown timer
		
		Vector2 input = Vector2.zero;
		if (usingController == 0) input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));
		//keyboard input
		if (usingController == 1) {
			float tempJoystick = Input.GetAxis ("JoystickLookLeftRight") / (Mathf.Abs (Input.GetAxis ("JoystickLookLeftRight")) + 8f);
			initialTurn -= tempJoystick;
			mouseDrag = Mathf.Abs (tempJoystick);
			//this is just equals, if we only have the joystick. We started it with mouse
			tempJoystick = Input.GetAxis ("JoystickLookUpDown") / (Mathf.Abs (Input.GetAxis ("JoystickLookUpDown")) + 8f);
			initialUpDown += tempJoystick;
			tempJoystick = -tempJoystick;
			if (Mathf.Abs (tempJoystick) > mouseDrag)
				mouseDrag = Mathf.Abs (tempJoystick);
			mouseDrag /= 4f;
			//compensating for differences in input systems: see the tilt code for another case of this change
			if (initialTurn < -clampRotateAngle)
				initialTurn += clampRotateAngle;
			if (initialTurn > clampRotateAngle)
				initialTurn -= clampRotateAngle;
			lookAngleUpDown = Mathf.Lerp (lookAngleUpDown, Mathf.Abs (Input.GetAxis ("JoystickLookUpDown")) + 0.5f, Mathf.Abs (Input.GetAxis ("JoystickLookUpDown") * 0.1f) + 0.2f);
			//this is clamping the max look angle so if you're physically pushing the stick it lets you look farther. release the stick and it goes partway back again
			initialUpDown = Mathf.Clamp (initialUpDown, -lookAngleUpDown, lookAngleUpDown);
			input = new Vector2 (Mathf.Pow (Input.GetAxis ("JoystickMoveForwardBack"), 5f), Mathf.Pow (Input.GetAxis ("JoystickStrafeLeftRight"), 5f));
		} //joystick is a lot more complicated, but necessary to make controller responsive
		
		Vector3 groundContactNormal;
		Vector3 rawMove = mainCamera.transform.forward * input.y + mainCamera.transform.right * input.x;
		float adjacentSolid = 99999;
		float downSolid = 99999;

		releaseJump = true;
		//it's FixedUpdate, so release the jump in Update again so it can be retriggered.

		particlesystem.transform.localPosition = Vector3.forward * (1f + (rigidBody.velocity.magnitude * Time.fixedDeltaTime));
		if (Input.GetButton ("Talk") || Input.GetButton ("KeyboardTalk") || Input.GetButton ("MouseTalk")) {
			if (packets > 0) {
				if (!particlesystem.isPlaying) particlesystem.Play ();
				particlesystem.Emit(1);
				packets -= 1;
			}
		}
		//this too can be fired by either system with no problem
		

		if (Physics.SphereCast (transform.position, sphereCollider.radius, Vector3.down, out hit, 99999f, onlyTerrains)) {
			groundContactNormal = hit.normal;
			desiredMove = Vector3.ProjectOnPlane (rawMove, groundContactNormal).normalized;
			//set this up here so the fuel can take advantage of the ground proximity
		} else {
			groundContactNormal = Vector3.up;
			desiredMove = Vector3.ProjectOnPlane (rawMove, groundContactNormal).normalized;
		}


		pingTimer += 1;
		if (pingTimer > yourMatchDistance) pingTimer = 0;

		if ((audiosource.clip != botBeep) && audiosource.isPlaying) {
			//we are playing the smashing sounds of the giant guardians
		} else {
			audiosource.pitch = 3f / ((pingTimer + 64f) / 65f);
			if (pingTimer == 0) {
				if (ourlevel.GetComponent<SetUpBots> ().gameEnded == false) {
					float pingVolume = 1f / Mathf.Sqrt(yourMatchDistance);
					if (audiosource.clip != botBeep)
						audiosource.clip = botBeep;
					if (yourMatchOccluded) {
						audiosource.volume = pingVolume;
						audiosource.reverbZoneMix = 1.9f;
					} else {
						audiosource.volume = pingVolume;
						//we will keep it the same so it sounds the same: increasing volume
						//caused it to sound different inc. in the reverb
						float verbZone = 2f - (70f / yourMatchDistance);
						if (verbZone < 0f)
							verbZone = 0f;
						audiosource.reverbZoneMix = verbZone;
					}
					audiosource.Play ();
				}
			}
			//this is our geiger counter for our bot
		}

		if (Physics.Raycast (transform.position, Vector3.down, out hit, 99999f, onlyTerrains)) {
			altitude = hit.distance;
			if (adjacentSolid > altitude)
				adjacentSolid = altitude;
			if (downSolid > altitude)
				downSolid = altitude;
			//down's special, use it to test for climbing walls with jumps
		} else {
			if (Physics.Raycast (transform.position + (Vector3.up * 9999f), Vector3.down, out hit, 99999f, onlyTerrains)) {
				transform.position = hit.point + Vector3.up;
				rigidBody.velocity += Vector3.up;
				altitude = 1;
			}
		}

		if (Physics.Raycast (transform.position, Vector3.left, out hit, 99f, onlyTerrains)) {
			if (adjacentSolid > hit.distance)
				adjacentSolid = hit.distance;
		}
		if (Physics.Raycast (transform.position, Vector3.right, out hit, 99f, onlyTerrains)) {
			if (adjacentSolid > hit.distance)
				adjacentSolid = hit.distance;
		}
		if (Physics.Raycast (transform.position, Vector3.forward, out hit, 99f, onlyTerrains)) {
			if (adjacentSolid > hit.distance)
				adjacentSolid = hit.distance;
		}
		if (Physics.Raycast (transform.position, Vector3.back, out hit, 99f, onlyTerrains)) {
			if (adjacentSolid > hit.distance)
				adjacentSolid = hit.distance;
		}
		//this gives us a quick nearest-surface check for all directions but up
		
		if (adjacentSolid < 1) {
			float bumpUp = transform.position.y + ((1 - adjacentSolid) / 32);
			transform.position = new Vector3 (transform.position.x, bumpUp, transform.position.z);
			//this keeps us off the ground
			adjacentSolid = 1;
		}
		//thus we can only maneuver if we are near a surface
		
		adjacentSolid *= adjacentSolid;
		adjacentSolid *= adjacentSolid;
		
		if (desiredMove.y > 0) {
			float angle = Vector3.Angle (groundContactNormal, Vector3.up);
			moveLimit = slopeCurveModifier.Evaluate (angle);
			//apply a slope based limiting factor
			
			desiredMove *= moveLimit;
			desiredMove.y *= moveLimit;
			//we can't climb but we can go down all we want
			//try to restrict vertical movement more than lateral movement
		}

		float momentum = Mathf.Sqrt(Vector3.Angle (mainCamera.transform.forward, rigidBody.velocity)+4f+mouseDrag) * 0.1f;
		//4 controls the top speed, 0.1 controls maximum clamp when turning
		if (momentum < 0.001f) momentum = 0.001f; //insanity check
		if (momentum > adjacentSolid) momentum = adjacentSolid; //insanity check
		if (adjacentSolid < 1f) adjacentSolid = 1f; //insanity check
		desiredMove /= (adjacentSolid + mouseDrag);
		//we're adding the move to the extent that we're near a surface
		rigidBody.drag = momentum / adjacentSolid; //1f + mouseDrag
		//alternately, we have high drag if we're near a surface and little in the air
		
		rigidBody.AddForce (desiredMove, ForceMode.Impulse);

		stepsBetween = 0f;
		//zero out the step-making part and start over
		startPosition = Vector3.zero;
		endPosition = rigidBody.velocity * Time.fixedDeltaTime;
		//we see if this will work. Certainly we want to scale it to fixedDeltaTime as we're in FixedUpdate
		
		if (Physics.Raycast (transform.position, Vector3.down, out hit) == false) {
			playerPosition = transform.position;
			playerPosition.y = 99999f;
			if (Physics.Raycast (playerPosition, Vector3.down, out hit) == true) {
				playerPosition = hit.point + Vector3.up;
			} else {
				playerPosition = new Vector3 (1614f, 9999f, 2083f);
				//if all else fails, drop from the sky in the middle of the map
			}
			lastPlayerPosition = transform.position = playerPosition;
		}
		//insanity check to stop falling into infinity


		if (Vector3.Distance (transform.position, lastPlayerPosition) > 5f) {
			transform.position = lastPlayerPosition;
			rigidBody.velocity = Vector3.zero;
			endPosition = startPosition;
		}
 		else lastPlayerPosition = transform.position;
		//insanity check: if for any reason we've moved faster than 5 world units per tick, the dreaded geometry glitch has struck
		//and so we don't move from the last good place, and we zero velocity and see if that does any good.
	}



	IEnumerator SlowUpdates () {
		while (true) {
			if (transform.position.x < 0f) {
				if (Vector3.Distance (transform.position, guardian.transform.position) < 600f) {
					guardian.transform.position = new Vector3 (guardian.transform.position.x + 4000f, guardian.transform.position.y, guardian.transform.position.z);
					guardianmovement.locationTarget.x += 4000f;
				}
				transform.position = new Vector3 (transform.position.x + 4000f, transform.position.y, transform.position.z);
				playerPosition = lastPlayerPosition = transform.position;
			}

			if (Mathf.Abs (Input.GetAxis ("JoystickLookUpDown")) > 1f)
				usingController = 1;
			if (Mathf.Abs (Input.GetAxis ("JoystickMoveForwardBack")) > 1f)
				usingController = 1;
			if (Mathf.Abs (Input.GetAxis ("Horizontal")) > 1f)
				usingController = 0;
			if (Mathf.Abs (Input.GetAxis ("Vertical")) > 1f)
				usingController = 0;
			//trying to switch stuff based on what controller is in use. Mouse overrides if used

			if (Cursor.lockState != CursorLockMode.Locked) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			//the notorious cursor code! Kills builds on Unity 5.2 and up

			if (setupbots.gameEnded && (Input.GetButton ("Next") || Input.GetButton ("KeyboardNext"))) {
				//trigger new level load on completing of level
				//we have already updated the score and saved prefs
				Application.LoadLevel ("Scene");
			}

			timeBetweenGuardians *= 0.999f;
			//with this factor we scale how sensitive guardians are to bots bumping each other

			if (lastpackets != packets) {
				lastpackets = packets;
				packetdisplay.sizeDelta = new Vector2 (35f, (packets / 1000f) * (Screen.height - 24f));
			}
			speed = (int)(rigidBody.velocity.magnitude * 7f);
			if (lastspeed != speed) {
				lastspeed = speed;
				speeddisplay.sizeDelta = new Vector2 (35f, 135f + ((speed / 1000f) * Screen.height));
			}
			//updating the meters needn't be 60fps and up
		
			yield return playerWait;

			cameraZoom = (Mathf.Sqrt (rigidBody.velocity.magnitude + 2f) * 2f) + (initialUpDown*4f) + (playerPosition.y/200f);
			//elaborate zoom goes wide angle for looking up, and for high ground

			if (altitude < 1f) {
				backgroundSound.whooshLowCut = Mathf.Lerp (backgroundSound.whooshLowCut, 0.001f, 0.5f);
				backgroundSound.whoosh = (rigidBody.velocity.magnitude * Mathf.Sqrt (rigidBody.velocity.magnitude) * 0.000008f);
			} else {
				backgroundSound.whooshLowCut = Mathf.Lerp (backgroundSound.whooshLowCut, 0.2f, 0.5f);
				backgroundSound.whoosh = (rigidBody.velocity.magnitude * Mathf.Sqrt (rigidBody.velocity.magnitude) * 0.000005f);
			}

			if (botNumber < totalBotNumber) {
				ourlevel.GetComponent<SetUpBots> ().SpawnBot (-1, false);
			} //generate a bot if we don't have 500 and our FPS is at least 50. Works for locked framerate too as that's bound to 60
			//uses totalBotNumber because if we start killing them, the top number goes down!

			if (transform.position.z < 0f) {
				if (Vector3.Distance (transform.position, guardian.transform.position) < 600f) {
					guardian.transform.position = new Vector3 (guardian.transform.position.x, guardian.transform.position.y, guardian.transform.position.z + 4000f);
					guardianmovement.locationTarget.z += 4000f;
				}
				transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z + 4000f);
				playerPosition = lastPlayerPosition = transform.position;
			}
			mainCamera.fieldOfView = baseFOV + (cameraZoom * 0.5f);
			backgroundSound.brightness = (transform.position.y / 900.0f) + 0.2f;

			yield return playerWait;

			if (transform.position.x > 4000f) {
				if (Vector3.Distance (transform.position, guardian.transform.position) < 600f) {
					guardian.transform.position = new Vector3 (guardian.transform.position.x - 4000f, guardian.transform.position.y, guardian.transform.position.z);
					guardianmovement.locationTarget.x -= 4000f;
				}
				transform.position = new Vector3 (transform.position.x - 4000f, transform.position.y, transform.position.z);
				playerPosition = lastPlayerPosition = transform.position;
			}
			wireframeCamera.fieldOfView = baseFOV + (cameraZoom * 0.5f);
			float recip = 1.0f / backgroundSound.gain;
			recip = Mathf.Lerp ((float)recip, altitude, 0.5f);
			recip = Mathf.Min (100.0f, Mathf.Sqrt (recip + 12.0f));

			if (botNumber < totalBotNumber) {
				ourlevel.GetComponent<SetUpBots> ().SpawnBot (-1, false);
			} //generate a bot if we don't have 500 and our FPS is at least 30. Works for locked framerate too as that's bound to 60

			yield return playerWait;

			if (transform.position.z > 4000f) {
				if (Vector3.Distance (transform.position, guardian.transform.position) < 600f) {
					guardian.transform.position = new Vector3 (guardian.transform.position.x, guardian.transform.position.y, guardian.transform.position.z - 4000f);
					guardianmovement.locationTarget.z -= 4000f;
				}
				transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z - 4000f);
				playerPosition = lastPlayerPosition = transform.position;
			}
			///walls! We bounce off the four walls of the world rather than falling out of it

			skyboxRot -= 0.08f;
			if (skyboxRot < 0f)
				skyboxRot += 360f;
			overlayBoxMat.SetFloat ("_Rotation", skyboxRot);
			skyboxRot2 += 0.06f;
			if (skyboxRot > 360f)
				skyboxRot -= 360f;
			overlayBoxMat2.SetFloat ("_Rotation", skyboxRot2);
			//try to rotate the skybox
			skyboxCamera.fieldOfView = baseFOV + cameraZoom;

			backgroundSound.gain = 1.0f / recip;

			botNumber = allbots.transform.childCount;
			if (botNumber > totalBotNumber)
				botNumber = totalBotNumber;
			//it insists on finding gameObjects when we've killed bots, so we force it to be what we want
			//with this we can tweak sensitivity to things like bot "activityRange"

			deltaTime += (Time.deltaTime - deltaTime) * 0.01f;
					
			creepToRange -= (0.01f + (0.00001f * levelNumber));
			//as levels advance, we get the 'bot party' a lot more often and they get busier running into the center and back out
			if (creepToRange < 1f) {
				creepToRange = (float)Mathf.Min (1800, levelNumber);
				creepRotAngle = UnityEngine.Random.Range (0f, 359f);
				//each time, the whole rotation of the 'bot map' is different.
			}
			//bots cluster closer and closer into a big bot party, until suddenly bam! They all flee to the outskirts. Then they start migrating in again.

			if (botNumber < totalBotNumber) {
				ourlevel.GetComponent<SetUpBots> ().SpawnBot (-1, false);
			} //generate a bot if we don't have 500 and our FPS is at least 58. Works for locked framerate too as that's bound to 60
			//uses totalBotNumber because if we start killing them, the top number goes down!
			//thus, if we have insano framerates, the bots can spawn incredibly fast, but it'll sort of ride the wave if it begins to chug

			if (notStartedColorBitsScreen) {
				colorBits.Apply ();
				colorDisplay.SetMaterial (colorDisplay.GetMaterial (), colorBits);
				notStartedColorBitsScreen = false;
			}

			yield return playerWait;
		}
	}
}

