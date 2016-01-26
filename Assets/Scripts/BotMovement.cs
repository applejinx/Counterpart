﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]

public class BotMovement : MonoBehaviour
{
	public Vector3 botTarget;
	public Mesh meshLOD0;
	public Mesh meshLOD1;
	public Mesh meshLOD2;
	public Mesh meshLOD3;
	public Mesh meshLOD4;
	public int yourMatch;
	public bool withinRange;
	private Vector3 rawMove;
	private Vector3 desiredMove;
	private Vector3 lerpedMove = Vector3.zero;
	private Vector3 groundContactNormal;
	public AudioClip BotCrash;
	public AudioClip BotCrashTinkle;
	public AudioClip BotBeep;
	public AudioClip happyEnding;
	private bool notEnded;
	private AudioSource audioSource;
	public Color32[] botBrain;
	public int brainPointer = 0;
	public int voicePointer = 0;
	public int jumpCounter;
	private float altitude = 1f;
	private float adjacentSolid = 99999;
	private int brainR;
	private int brainG;
	private int brainB;
	public int step;
	private SphereCollider sphereCollider;
	private Rigidbody rigidBody;
	private float prevVelocityY = 0f;
	private GameObject dolly;
	private Vector3 startPosition;
	private Vector3 endPosition;
	private float stepsBetween;
	private LayerMask onlyTerrains;
	private LayerMask otherBots;
	private MeshFilter meshfilter;
	private Renderer myColor;
	private RaycastHit hit;
	private GameObject ourhero;
	private Rigidbody ourheroRigidbody;
	private PlayerMovement playermovement;
	private GuardianMovement guardianNmovement;
	private GameObject level;
	private SetUpBots setupbots;
	private GameObject guardianN;
	private GameObject guardianS;
	private GameObject logo;
	private GameObject botZaps;
	private ParticleSystem botZapsParticles;
	private Vector3 overThere;
	private bool watchingForYou;
	private float distance;
	private float squish = 1f;
	private float squishRecoil = 0f;
	private float squosh = 1f;

	void Awake ()
	{
		rigidBody = GetComponent<Rigidbody> ();
		sphereCollider = GetComponent<SphereCollider> ();
		dolly = transform.FindChild ("Dolly").gameObject;
		meshfilter = dolly.GetComponent<MeshFilter> ();
		myColor = dolly.GetComponent<Renderer> ();
		audioSource = GetComponent<AudioSource> ();
		ourhero = GameObject.FindGameObjectWithTag ("Player");
		ourheroRigidbody = ourhero.GetComponent<Rigidbody> ();
		playermovement = ourhero.GetComponent<PlayerMovement> ();
		level = GameObject.FindGameObjectWithTag ("Level");
		setupbots = level.GetComponent<SetUpBots> ();
		guardianN = GameObject.FindGameObjectWithTag ("GuardianN");
		guardianNmovement = guardianN.GetComponent<GuardianMovement> ();
		//we assign this so that the bot can keep sending whatever location data to whichever AI
		//we'll check for null to see if it's ever been directed to a target.
		//should be pretty cheap to keep references for this stuff around
		//using this, we can punch a target or chase behavior into a specific guardian,
		//without it having to go through all the bots when it needs to react to a specific bot.
		logo = GameObject.FindGameObjectWithTag ("counterpartlogo");
		botZaps = GameObject.FindGameObjectWithTag ("Line");
		botZapsParticles = botZaps.GetComponent<ParticleSystem> ();
		onlyTerrains = 1 << LayerMask.NameToLayer ("Wireframe");
		otherBots = 1 << LayerMask.NameToLayer ("Default");
		notEnded = true;
		watchingForYou = false;
		overThere = Vector3.zero;
	}

	void OnCollisionEnter (Collision col)
	{
		jumpCounter -= 1;
		//no matter what, if we collide we trigger the jump counter
		if (col.gameObject.tag == "Player" && notEnded) {
			if (col.relativeVelocity.magnitude > 30f) {
				playermovement.timeBetweenGuardians = 1f;
				playermovement.dollyOffset = col.relativeVelocity.magnitude/200f;
				//hitting a bot knocks your viewpoint
				//you bonked a bot so the guardian will get between you
				watchingForYou = false;
				overThere = Vector3.zero;
				//you pissed it off and it won't help you until you re-ask
				brainPointer += 1;
				if (brainPointer >= botBrain.Length)
					brainPointer = 0;
				//bots hit hard enough to crash get discombot-ulated
				audioSource.clip = BotCrash;
				audioSource.reverbZoneMix = 0f;
				audioSource.pitch = 3f - ((col.relativeVelocity.magnitude - 25f) * 0.01f);
				audioSource.volume = 0.5f;
				PlayerMovement.guardianHostility += 0.01f;
				guardianNmovement.guardianCooldown += ((col.relativeVelocity.magnitude / 10f) * (ourheroRigidbody.velocity.magnitude * 0.025f));
				//lerp value, slowly diminishes. Multiple kills can make a guardian super aggressive. Or if you are super speeding
				guardianNmovement.locationTarget = transform.position;
				//whether or not we killed the other bot, we are going to trigger the guardian

				if ((col.relativeVelocity.magnitude > 40f) && (setupbots.gameEnded == false)) {
					//if you've won the game you can't kill bots, it would lack class
					audioSource.clip = BotCrashTinkle;
					audioSource.reverbZoneMix = 0f;
					audioSource.pitch = 1.0f - ((col.relativeVelocity.magnitude) * 0.0005f);
					if (audioSource.pitch < 0.2f)
						audioSource.pitch = 0.2f;
					audioSource.volume = 1f;
					if (playermovement.yourMatch == yourMatch) {
						myColor.material.color = new Color (0.35f, 0.35f, 0.35f);
						sphereCollider.material.staticFriction = 0.2f;
						Destroy (this);
						//REKKT. Bot's brain is destroyed and since it was your soulmate...
						ourhero.GetComponent<SphereCollider> ().material.staticFriction = 0.2f;
						ourheroRigidbody.freezeRotation = false;
						ourheroRigidbody.angularDrag = 0.6f;
						setupbots.gameEnded = true;
						setupbots.killed = true;
						Destroy (playermovement);
						logo.GetComponent<Text> ().text = "Game Over";
						//you are REKKT too!
						audioSource.clip = BotCrashTinkle;
						audioSource.reverbZoneMix = 0f;
						audioSource.pitch = 0.08f;
						audioSource.volume = 1f;
						//override with an epic fail crash
					} else {
						myColor.material.color = new Color (0.3f, 0.3f, 0.3f);
						sphereCollider.material.staticFriction = 0.2f;
						rigidBody.freezeRotation = false;
						rigidBody.angularDrag = 0.5f;
						Destroy (this);
						//REKKT. Bot's brain is destroyed, after setting its color to dim.
						playermovement.totalBotNumber = playermovement.totalBotNumber - 1;
						PlayerMovement.guardianHostility += 0.1f;
					}//when over 40, decide if you kill entire game or just the other bot.
				}//also over 25
				audioSource.Play ();
				//play if over 15 or more
			} else {
				if (playermovement.yourMatch == yourMatch) {
					rigidBody.velocity = Vector3.zero;
					lerpedMove = Vector3.zero;
					ourheroRigidbody.velocity = Vector3.zero;
					//freeze, in shock and delight!

					AudioSource externalSource = GameObject.FindGameObjectWithTag ("overheadLight").GetComponent<AudioSource> ();
					externalSource.Stop ();
					externalSource.clip = happyEnding;
					externalSource.pitch = 1f;
					externalSource.volume = 1f;
					externalSource.reverbZoneMix = 0f;
					externalSource.spatialBlend = 0f;
					//switch the earthquake FX to normal stereo, music playback
					//That ought to fix the end music cutoff, it checks to see if each earthquake is done already
					externalSource.PlayOneShot (happyEnding, 1f);
					logo.GetComponent<Text> ().text = "Success!";
					playermovement.dollyOffset = 3.0f;
					notEnded = false;
					//with that, we switch off the bot this is
					setupbots.gameEnded = true;
				} else {
					voicePointer += 1;
					if (voicePointer >= botBrain.Length)
						voicePointer = 0;
					int left = Math.Abs (playermovement.yourBrain [voicePointer].r - botBrain [voicePointer].r);
					int right = Math.Abs (playermovement.yourBrain [voicePointer].g - botBrain [voicePointer].g);
					int center = Math.Abs (playermovement.yourBrain [voicePointer].b - botBrain [voicePointer].b);
					if (notEnded && withinRange) {
						if (audioSource.clip != BotBeep)
							audioSource.clip = BotBeep;
						audioSource.volume = 2f / Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
						audioSource.reverbZoneMix = 0.01f;
						float voicePitch = 2.9f - ((center + left + right) * 0.006f);
						if (voicePitch > 0f)
							audioSource.pitch = voicePitch;
						if (!audioSource.isPlaying)
							audioSource.Play ();
					}
					//bot talks without interrupting its dance, if you're gentle
				}
			} //decide if it's a hit or a kiss
			//with the player
		} else {
			if (col.gameObject.tag == "Terrain") {
				squish = (1f / (Mathf.Abs((prevVelocityY-rigidBody.velocity.y)*0.02f)+1f)) - 1f;
				squishRecoil = 0f;
				//try to splat when landing on terrain
			}

			//bots hitting bots here
			PlayerMovement.guardianHostility -= 0.01f;
			if (PlayerMovement.guardianHostility < 0f) PlayerMovement.guardianHostility = 0f;
	
			if (col.relativeVelocity.magnitude > (playermovement.timeBetweenGuardians * 400f)) {
				//amount of this gives us how active the guardians are
				audioSource.clip = BotCrash;
				audioSource.pitch = 3f - ((col.relativeVelocity.magnitude - 25f) * 0.01f);
				audioSource.volume = 0.5f;
				playermovement.timeBetweenGuardians = 1f;
				//reset the guardian sensitivity
				guardianNmovement.guardianCooldown += ((col.relativeVelocity.magnitude / 10f) * (rigidBody.velocity.magnitude * 0.025f));
				//lerp value, slowly diminishes. Inter-bot hits can't make the guardian target you
				guardianNmovement.locationTarget = transform.position;
				//whether or not we killed the other bot, we are going to trigger the guardian
			} //if the collision is hard, bots crash and the guardian goes to see them

			BotMovement botmovement = col.gameObject.GetComponent<BotMovement> ();
			if (botmovement != null) {
				if (botmovement.yourMatch == yourMatch) {
					botmovement.brainPointer = brainPointer;
					botmovement.step = 9999;
					//step is always set to what will engage the brain and do a new pointer
					if (!Physics.Linecast (transform.position, ourhero.transform.position, onlyTerrains))
					if (notEnded && withinRange) {
						if (audioSource.clip != BotBeep)
							audioSource.clip = BotBeep;
						audioSource.volume = 3f / Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
						audioSource.reverbZoneMix = 0.01f;
						brainR = botBrain [brainPointer].r;
						brainG = botBrain [brainPointer].g;
						brainB = botBrain [brainPointer].b;
						audioSource.reverbZoneMix = Vector3.Distance (transform.position, ourhero.transform.position) / 480f;
						float voicePitch = 2.9f - ((brainR + brainG + brainB) * 0.006f);
						if (voicePitch > 0f)
							audioSource.pitch = voicePitch;
						if (!audioSource.isPlaying)
							audioSource.Play ();
						//bot makes a remark, unless it's too far to hear
					}
				}
				//upon hitting another bot, if they're the same, they sync brainwaves.
			}
			//with another bot
		} 
	} //entire collision

	void OnParticleCollision (GameObject shotBy)
	{
		voicePointer += 1;
		if (voicePointer >= botBrain.Length)
			voicePointer = 0;
		int left = Math.Abs (playermovement.yourBrain [voicePointer].r - botBrain [voicePointer].r);
		int right = Math.Abs (playermovement.yourBrain [voicePointer].g - botBrain [voicePointer].g);
		int center = Math.Abs (playermovement.yourBrain [voicePointer].b - botBrain [voicePointer].b);
		if (notEnded && withinRange) {
			if (audioSource.clip != BotBeep)
				audioSource.clip = BotBeep;
			audioSource.volume = 2f;
			audioSource.reverbZoneMix = 0.01f;
			float voicePitch = 2.9f - ((center + left + right) * 0.003f);
			if (voicePitch > 0f)
				audioSource.pitch = voicePitch;
			audioSource.Play ();

			if (Vector3.Distance (transform.position, ourhero.transform.position) < 200f) watchingForYou = true;
			//if we're near enough that the bot knows you're the one asking, then it will watch for your counterpart

			if ((watchingForYou == true) && (overThere != Vector3.zero)) {
				botZaps.transform.position = transform.position;
				botZaps.transform.LookAt (overThere);
				if (playermovement.yourMatch == yourMatch) botZaps.transform.LookAt (botZaps.transform.position + Vector3.up);
				botZapsParticles.startSize = 3f;
				botZapsParticles.Emit(1);
			}
			//will fire a particle in the direction of where it last saw the one we want, if it's seen the bot in question, and if it is not that bot
		}
		rigidBody.velocity = Vector3.Lerp (rigidBody.velocity, Vector3.Lerp (ourheroRigidbody.velocity, Vector3.zero, 0.4f), (botBrain [voicePointer].g / 255f));
		//bots that are more than 50% G (greens and whites) are cooperative and stop to talk. Dark or nongreen bots won't.
	}

	void Update ()
	{
		dolly.transform.localPosition = Vector3.Lerp (startPosition, endPosition, Time.deltaTime * stepsBetween);
		stepsBetween += Time.deltaTime / Time.fixedDeltaTime;
	}

	void FixedUpdate ()
	{
		squishRecoil -= (squish * 0.128f);
		squish = (squish + squishRecoil) * 0.7f;

		squosh = 1f - (squish * 0.6283f); //expand out to the sides
		transform.localScale = new Vector3 (squosh, squish + 1f, squosh);

		//FixedUpdate is run as many times as needed, before an Update step: or, it's skipped if framerate is super high.		
		adjacentSolid = 99999;

		if (Physics.SphereCast (transform.position, sphereCollider.radius, Vector3.down, out hit, sphereCollider.radius))
			groundContactNormal = hit.normal;
		else
			groundContactNormal = Vector3.up;

		rawMove = botTarget - transform.position;


		desiredMove = Vector3.ProjectOnPlane (rawMove, groundContactNormal).normalized;
		//this is where we're applying the desired move of the bot. Since it normalizes it, we can make
		//rawMove any damn thing we want, it doesn't matter
		
		if (Physics.Raycast (transform.position, Vector3.down, out hit, 9999f, onlyTerrains)) {
			altitude = hit.distance;
			if (adjacentSolid > altitude)
				adjacentSolid = altitude;
		} else {
			if (Physics.Raycast (transform.position + (Vector3.up * 9999f), Vector3.down, out hit)) {
				transform.position = hit.point + Vector3.up;
				rigidBody.velocity += Vector3.up;
				altitude = 1;
			}
		}
		//bot's basic height off ground

		if ((rigidBody.velocity.magnitude) < (rawMove.magnitude * 0.004)) {
			desiredMove *= 0.4f; //at one, even a single one of these makes 'em levitate
			if (rawMove.magnitude > 1000f) rigidBody.AddForce (desiredMove, ForceMode.Impulse);
			if (rawMove.magnitude > 2000f) rigidBody.AddForce (desiredMove, ForceMode.Impulse);
			//they go like maniacs when they have to go very far
		}
		//we're gonna try to identify when they're stuck on something and let them jump their way out of it

		if (adjacentSolid < 1f)
			adjacentSolid = 1f;
		adjacentSolid *= adjacentSolid;
		rigidBody.drag = 1f / adjacentSolid;
		adjacentSolid *= adjacentSolid;
		//bot has high drag if near the ground and little in the air
		desiredMove /= adjacentSolid;
		//we're adding the move to the extent that we're near a surface

		desiredMove *= (0.5f + (0.00002f * (brainR + brainG)));
		//scale everything back depending on the R factor
		lerpedMove = Vector3.Lerp (lerpedMove, desiredMove, 0.001f + (0.0001f * brainR));
		//texture red makes the bots go more hyper!

		rigidBody.AddForce (lerpedMove / adjacentSolid, ForceMode.Impulse);
		//apply the attempted bot move as adjusted

		stepsBetween = 0f;
		//zero out the step-making part and start over
		startPosition = Vector3.zero;
		endPosition = rigidBody.velocity * Time.fixedDeltaTime;
		//we see if this will work. Certainly we want to scale it to fixedDeltaTime as we're in FixedUpdate

		if (jumpCounter < 0) {
			jumpCounter = (int)Math.Sqrt(brainB + brainG)+1;
			//purely red bots are jumpier
			rigidBody.AddForce (Vector3.up * 15f, ForceMode.Impulse);
			squish = 0.3f;
			squishRecoil = 0.3f;
			//jump!
		}

		prevVelocityY = rigidBody.velocity.y;

		if (botBrain.Length > 1)
			StartCoroutine ("SlowUpdates");
		//our heavier processing doesn't update quickly
		//we're testing to make sure it has a length, if not then it's a deleted bot
		//and we won't attempt to run any of that stuff
	}
	
	IEnumerator SlowUpdates ()
	{
		step += 1;
		step += (botBrain [brainPointer].r / 100);
		//red bots are more agitated, to a point.

		if (audioSource.pitch < 0f)
			audioSource.Stop ();
		//staccato: the bots can and do shorten their beeps. Green means perky short beeps, no green means longer

		if (transform.position.x < 0f) {
			transform.position = new Vector3 (transform.position.x + 4000f, transform.position.y, transform.position.z);
		}

		if (Physics.Linecast (transform.position, (playermovement.locationOfCounterpart + (transform.position - playermovement.locationOfCounterpart).normalized)) == false) {
			//returns true if there's anything in the way. If there is nothing 
			//(may need to offset the destination as we might hit the lucky bot)
			overThere = playermovement.locationOfCounterpart;
			//now the bot knows where to emit particles when asked!
		}

		yield return new WaitForSeconds (.01f);
		//walls! We bounce off the four walls of the world rather than falling out of it

		
		if (transform.position.z < 0f) {
			transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z + 4000f);
		}

		if (setupbots.gameEnded && (!setupbots.killed))
			botTarget = ourhero.transform.position;
		//if we won, hooray! Everybody pile on the lucky bot! :D

		if (step >= botBrain [brainPointer].b) {
			step = 0;
			//here's where we do the bot manevuerings
			//note that step will always start at zero, so we can go above to where it's updated
			//and use it as our staccato mechanism
			//at 16386 slots, three updates a second, it will take 90 minutes to get through
			//blue is more serene!
			brainPointer += 1;
			if (brainPointer >= botBrain.Length)
				brainPointer = 0;
			brainR = botBrain [brainPointer].r;
			brainG = botBrain [brainPointer].g;
			brainB = botBrain [brainPointer].b;
			//we establish a new target location based on this color

			if (Physics.Linecast (transform.position, botTarget, otherBots) && !setupbots.gameEnded) {
				botZaps.transform.position = transform.position;
				botZaps.transform.LookAt (botTarget);
				botZapsParticles.startSize = 0.4f;
				botZapsParticles.Emit (1);
				//and fires a particle if it's looking at another bot
				//and then it beeps, either verbed or not
				if (!Physics.Linecast (transform.position, ourhero.transform.position, onlyTerrains)) {
					if (notEnded && withinRange) {
						if (audioSource.clip != BotBeep)
							audioSource.clip = BotBeep;
						audioSource.volume = 3f / Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
						audioSource.reverbZoneMix = 0.01f;
						float voicePitch = 2.9f - ((brainR + brainG + brainB) * 0.006f);
						if (voicePitch > 0f)
							audioSource.pitch = voicePitch;
						if (!audioSource.isPlaying)
							audioSource.Play ();
						//bot makes a remark
					} else
						audioSource.Stop ();
				} else {
					//we have occluded bots. Verb them!
					if (notEnded && withinRange) {
						if (audioSource.clip != BotBeep)
							audioSource.clip = BotBeep;
						audioSource.volume = 2f / Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
						audioSource.reverbZoneMix = 1f - audioSource.volume;
						float voicePitch = 2.9f - ((brainR + brainG + brainB) * 0.006f);
						if (voicePitch > 0f)
							audioSource.pitch = voicePitch;
						if (!audioSource.isPlaying)
							audioSource.Play ();
						//bot makes a remark
					} else
						audioSource.Stop ();
				}
			}
			rigidBody.angularVelocity = Vector3.zero;
			if (Mathf.Abs(squish) < 0.01f) transform.LookAt (transform.localPosition + new Vector3 (brainR - 127f, brainG - 127f, brainB - 127f));
			else transform.LookAt (transform.localPosition + new Vector3 (brainR - 127f, 0f, brainB - 127f));
			//rotate only, but stay vertical for the bounce animations. Do full pivothead thing when not bouncing or landing
			Color c = new Color (brainR, brainG, brainB);
			SetUpBots.HSLColor color = SetUpBots.HSLColor.FromRGBA (c);
			//this is giving us 360 degree hue, and then saturation and luminance.
			float botDistance = Mathf.Abs (1f - color.s) * playermovement.creepToRange;
			float adjustedHueAngle = color.h + playermovement.creepRotAngle;
			Vector3 spawnLocation = new Vector3 (1614f + (Mathf.Sin (Mathf.PI / 180f * adjustedHueAngle) * botDistance), 9999f, 2083f + (Mathf.Cos (Mathf.PI / 180f * adjustedHueAngle) * botDistance));
			//aim bot at target
			if (Physics.Raycast (spawnLocation, Vector3.down, out hit, 99999f, onlyTerrains))
				botTarget = hit.point + Vector3.up;
			else
				botTarget = spawnLocation;
		}
		yield return new WaitForSeconds (.01f);

		if (transform.position.x > 4000f) {
			transform.position = new Vector3 (transform.position.x - 4000f, transform.position.y, transform.position.z);
		}
		if (audioSource.volume > 0.5 && audioSource.isPlaying && notEnded)
			myColor.material.color = new Color (0.7f, 0.7f, 0.7f);
		else
			myColor.material.color = new Color (0.5f, 0.5f, 0.5f);
		//bots light up when they are talking to you or banging, but not to play the end music

		yield return new WaitForSeconds (.01f);

		if (transform.position.z > 4000f) {
			transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z - 4000f);
		}
		distance = Vector3.Distance (transform.position, ourhero.transform.position);

		if (distance < 25) {
			meshfilter.mesh = meshLOD0;
		} else {
			if (distance < 50) {
				meshfilter.mesh = meshLOD1;
			} else {
				if (distance < 100) {
					meshfilter.mesh = meshLOD2;
					withinRange = true;
				} else {
					if (distance < 200) {
						meshfilter.mesh = meshLOD3;
					} else {
						meshfilter.mesh = meshLOD4;
						withinRange = false;
					}
				}
			}
		}
		//rolling my own LOD. With this, the render thread is so low in overhead that there's no point optimizing further: we're physics bound.

		if (playermovement.yourMatch == yourMatch) {
			playermovement.yourMatchDistance = Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position)) * 2f;
			playermovement.yourMatchOccluded = false;
			playermovement.locationOfCounterpart = transform.position;
			//this bot is your one true bot and we don't delete it or move it. We send the distance value to the 'ping' routine.
			//also we update you with its location so bots looking for you can know if they see it.
			if (Physics.Linecast (transform.position, ourhero.transform.position, onlyTerrains))
				playermovement.yourMatchOccluded = true;
			//by doing this, we can see whether there's anything in the way of the ray between match and player
			//If they're the same, we are NOT occluded and therefore we can hear the sonar beep better.
		} else {
			if (distance < playermovement.activityRange) {
				audioSource.Stop ();
			}
			if (distance > (playermovement.fps * playermovement.cullRange)) {
				Destroy (this.transform.gameObject);
				//if we are out of range AND the framerate's an issue AND we are not the lucky bot (this area is only for the disposables)
				//then mark this whole bot for destruction! This can rein in some frame rate chugs. At 10 fps it's killing bots as close as 1000 away,
				//at full 60fps vSync you have to be 600 away to be culled. And of course unsynced rapidly makes them uncullable.
			}
		}
		yield return new WaitForSeconds (.01f);
		//this is also where we'll do dumb AI things that might be time consuming. We'll always return to this point but it takes a while
		//to iterate through it all
	}
}
