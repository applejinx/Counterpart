﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]

public class GuardianMovement : MonoBehaviour {

	private AudioSource audiosource;
	public AudioClip[] earthquakes;
	public AudioClip BotCrashTinkle;
	private AudioSource externalSource;
	private Rigidbody myRigidbody;
	private Material guardianCore;
	private Material guardianMiddle;
	private Material guardianSurface;
	private Material guardianAura;
	private float churnCoreVisuals = 1f;
	private float churnMiddleVisuals = 1f;
	private float churnSurfaceVisuals = 1f;
	private GameObject earthquakeLight;
	private GameObject ourhero;
	public Vector3 locationTarget;
	private Vector3 guardianMotion;
	public float guardianCooldown = 0f;
	private RaycastHit hit;
	private PlayerMovement playermovement;
	private SetUpBots setupbots;
	private GameObject level;
	private GameObject logo;
	private GameObject devnotes;

	WaitForSeconds guardianWait = new WaitForSeconds(0.01f);


	void Awake ()
	{
		audiosource = GetComponent<AudioSource>();
		myRigidbody = GetComponent<Rigidbody>();
		guardianCore = transform.Find ("Core").GetComponent<Renderer> ().material;
		guardianMiddle = transform.Find ("Middle Layer").GetComponent<Renderer> ().material;
		guardianSurface = transform.Find ("Surface Sphere").GetComponent<Renderer> ().material;
		guardianAura = transform.Find ("Aura").GetComponent<Renderer> ().material;
		earthquakeLight = GameObject.FindGameObjectWithTag ("overheadLight");
		externalSource = earthquakeLight.GetComponent<AudioSource> ();
		//the guardian's one of the more important sounds
		locationTarget = Vector3.zero;
		ourhero = GameObject.FindGameObjectWithTag ("Player");
		playermovement = ourhero.GetComponent<PlayerMovement>();
		level = GameObject.FindGameObjectWithTag ("Level");
		logo = GameObject.FindGameObjectWithTag ("counterpartlogo");
		devnotes = GameObject.FindGameObjectWithTag ("instructionScreen");
		setupbots = level.GetComponent<SetUpBots>();
		StartCoroutine ("SlowUpdates");
	}

	void OnCollisionEnter(Collision col) {
		float crashScale = Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
		if (setupbots.gameEnded == true) {
			guardianCooldown = 0f;
			//we aren't messing with it. Hopefully this can give a unbreaking end music play
		} else {
			if (!externalSource.isPlaying) {
				externalSource.clip = earthquakes [Random.Range (0, earthquakes.Length)];
				externalSource.pitch = 0.34f - (crashScale * 0.0033f);
				if (Physics.Linecast (transform.position, (ourhero.transform.position + (transform.position - ourhero.transform.position).normalized)) == false) {
					//returns true if there's anything in the way. false means line of sight.
					externalSource.reverbZoneMix = crashScale * 0.00022f;
					externalSource.volume = 8f / crashScale;
				} else {
					//occluded, more distant
					externalSource.reverbZoneMix = crashScale * 0.00044f;
					externalSource.volume = 4f / crashScale;
				}


				externalSource.priority = 3;
				externalSource.Play ();
			}
			if (col.gameObject.tag == "Player") {
				ourhero.GetComponent<SphereCollider> ().material.staticFriction = 0.2f;
				ourhero.GetComponent<Rigidbody> ().freezeRotation = false;
				ourhero.GetComponent<Rigidbody> ().angularDrag = 0.6f;
				setupbots.gameEnded = true;
				setupbots.killed = true;
				GameObject.FindGameObjectWithTag ("Level").GetComponent<AudioSource> ().Stop();

				Destroy (playermovement);
				externalSource.clip = BotCrashTinkle;
				externalSource.reverbZoneMix = 0f;
				externalSource.pitch = 0.08f;
				externalSource.priority = 3;
				externalSource.volume = 1f;
				externalSource.Play();
				//
				logo.GetComponent<Text>().text = "Game Over";
				devnotes.GetComponent<Text>().text = " ";
				guardianCooldown = 0f;
				PlayerPrefs.SetInt ("levelNumber", 1);
				PlayerPrefs.Save();
			}
			//player is unkillable if they've already won
		}
	} //entire collision


	void OnParticleCollision (GameObject shotBy)
	{
		guardianCooldown = 2f;
		if (shotBy.CompareTag ("playerPackets")) {
			locationTarget = ourhero.transform.position;
		}
	}

	
	void FixedUpdate () {
		churnCoreVisuals -= (0.001f + (guardianCooldown * 0.001f));
		if (churnCoreVisuals < 0f) churnCoreVisuals += 1f;
		churnMiddleVisuals -= (0.0012f + (guardianCooldown * 0.001f));
		if (churnMiddleVisuals < 0f) churnMiddleVisuals += 1f;
		churnSurfaceVisuals -= (0.0014f + (guardianCooldown * 0.001f));
		if (churnSurfaceVisuals < 0f) churnSurfaceVisuals += 1f;
		//the churning activity gets more intense as the thing animates

		guardianCore.mainTextureOffset = new Vector2 (0, churnCoreVisuals); //core is the high res one
		guardianMiddle.mainTextureOffset = new Vector2 (0, churnMiddleVisuals); //middle is a coarser layer
		guardianSurface.mainTextureOffset = new Vector2 (0, churnSurfaceVisuals); //surface is low-poly


		Color guardianGlow = new Color (1f, 1f, 1f, 0.1f + (guardianCooldown * guardianCooldown * 0.02f));
		guardianCore.SetColor("_TintColor", guardianGlow);

		guardianGlow = new Color (1f, 1f, 1f, 0.08f + (guardianCooldown * guardianCooldown * 0.01f));
		guardianMiddle.SetColor("_TintColor", guardianGlow);

		guardianGlow = new Color (1f, 1f, 1f, 0.06f + (guardianCooldown * guardianCooldown * 0.005f));
		guardianSurface.SetColor("_TintColor", guardianGlow);

		guardianGlow = new Color (1f, 1f, 1f, 0.04f + (guardianCooldown * guardianCooldown * 0.0025f));
		guardianAura.SetColor("_TintColor", guardianGlow);
		//note that it is NOT '_Color' that we are setting with the material dialog in Unity!
		}

	IEnumerator SlowUpdates () {
		while (true) {
			if (guardianCooldown > 1f)
				guardianCooldown -= 0.02f;
			if (churnCoreVisuals * 3f < guardianCooldown)
				locationTarget = ourhero.transform.position;
			//alternate way to deal with hyper guardians?
			yield return guardianWait;

			Vector3 rawMove = locationTarget - transform.position;
			rawMove = rawMove.normalized * 40f * guardianCooldown;
			myRigidbody.AddForce (rawMove);
			if (guardianCooldown > 7f) myRigidbody.velocity *= 0.86f;
			if (guardianCooldown > 4f) guardianCooldown = 3f;
			//safeguard against crazy psycho zapping around

			if (guardianCooldown < 0f) {
				guardianCooldown = 0f;
				rawMove = locationTarget - transform.position;
				rawMove = rawMove.normalized * playermovement.terrainHeight;
				myRigidbody.AddForce (rawMove);
			}
			yield return guardianWait;

			float pitch = 0.5f / Mathf.Sqrt (Vector3.Distance (transform.position, ourhero.transform.position));
			audiosource.pitch = pitch;
			audiosource.priority = 4;
			audiosource.reverbZoneMix = 0.6f - pitch;
			float targetVolume = 0.05f;
			if (Physics.Linecast (transform.position, (ourhero.transform.position + (transform.position - ourhero.transform.position).normalized)) == false) {
				//returns true if there's anything in the way. false means line of sight.
				targetVolume = 0.15f;
			} else {
				//since there's something in the way, let's tame the beast
				guardianCooldown *= 0.93f;
			}
			if (setupbots.gameEnded == true)
				targetVolume = 0f;

			audiosource.volume = Mathf.Lerp (audiosource.volume, targetVolume, 0.1f);
			//ramp up fast but switch off more slowly.

			yield return guardianWait;
		}
	}
}
