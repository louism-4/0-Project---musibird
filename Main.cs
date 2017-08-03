using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.UI;
using DenisVizigin.Objects3D;
using UnityEngine;

namespace DenisVizigin.Sound 
{
	public class Main : MonoBehaviour
	{
		public double RadiansToDegrees = 57.29577951308232;
		public double CameraRotationEasing = 0.08;
		public double PlayerMovingEasing = 0.1;
		public float thresholdMultiplier = 1.5f;
		public int thresholdSize = -1;
		public double beatSensitivity = 1.5;
		public int beatSubbands = 3;
		public int sampleSize = 1024;
		public int soundFeed = 100;
		public int trackWidth = 1024;
		public int trackHeight = 22;
		public int trackDepth = 20000;
		public int cameraDistanceY = 900;
		public int cameraDistanceZ = 600;
		//---
	    
		private MusicAnalyzer analyzer;
		private AudioSource audio; 
		private double targetPlayerZ = 0.0;
		private double playerZ = 0.0;
		private int lastSample = 0;
	    private bool isAnalyzed = false;

		WWW myClip; string mySongString;

		public GameObject sphereRed;
		public GameObject sphereOrange;
		public GameObject sphereYellow;
		public GameObject sphereGreen;
		public GameObject sphereBlue;
		public GameObject spherePurple;
		public GameObject ring;

		public GameObject laser;

		public static GameObject slider;

		public GameObject player; GameObject home; GameObject menu; public GameObject text; GameObject ready; GameObject loading;
		GameObject coins; GameObject health;

	    void Start()
	    {
			health = GameObject.Find ("Health");
			health.SetActive (false);
			coins = GameObject.Find ("Coins");
			coins.SetActive (false);
			text = GameObject.Find("Title");
			mySongString = PlayerPrefs.GetString("song","");
			if(PlayerPrefs.GetInt("playList",0) == 1){
				text.GetComponent<Text>().text = mySongString;
				Debug.Log (mySongString);
				GetComponent<Camera>().GetComponent<AudioSource>().clip = (AudioClip)Resources.Load(mySongString);
			} 
			if (GetComponent<Camera>().GetComponent<AudioSource>().clip == null){
				GetComponent<Camera>().GetComponent<AudioSource>().clip = (AudioClip)Resources.Load("Tobu - Such Fun");
			} 
			home = GameObject.Find("Home");
			home.SetActive(false);
			menu = GameObject.Find("Menu");
			slider = GameObject.Find("LoadingSlider");
			slider.SetActive(false);
			Track.sphereRed = sphereRed;
			Track.sphereOrange = sphereOrange;
			Track.sphereYellow = sphereYellow;
			Track.sphereGreen = sphereGreen;
			Track.sphereBlue = sphereBlue;
			Track.spherePurple = spherePurple;
			Track.laser = laser; 
			Track.ring = ring;
			reInstantiate = false;
			foundTrue = false;
			executed = false;
			isAnalyzed = false;
	    	audio = GetComponent<Camera>().GetComponent<AudioSource>();
	        analyzer = new MusicAnalyzer(GetComponent<Camera>().GetComponent<AudioSource>().clip, sampleSize, soundFeed, beatSubbands, beatSensitivity, 
			                             thresholdSize, thresholdMultiplier);
	    }
		bool reInstantiate = false;
		bool foundTrue = false;
		bool executed = false;

		float time;
	    void Update()
	    {
			time += Time.deltaTime;
			if(MusicHandler.startLoad){
				if(GetComponent<AudioSource>().clip.isReadyToPlay && !GetComponent<AudioSource>().isPlaying){
					foundTrue = true;
				}
				MusicHandler.startLoad = false;
			}

	        if (isAnalyzed) {
				if(!executed){
					home.SetActive(true);
					Player3D.readyB = true;
					Score.ready = true;
					LightChanger.ready = true;
					changer.ready = true;
					Touch3D.ready = true;
					executed = true;
				}
				if (!audio.isPlaying) {
					audio.Play ();
				}
	        	UpdateTarget();
				UpdatePlayer();
				playerZ += PlayerMovingEasing * (targetPlayerZ - playerZ);
	        	UpdateCamera();
				return;
	        }
			if(foundTrue){
				if(!reInstantiate){
					slider.SetActive(true);
					analyzer = new MusicAnalyzer(GetComponent<Camera>().GetComponent<AudioSource>().clip, sampleSize, soundFeed, beatSubbands, beatSensitivity, 
						thresholdSize, thresholdMultiplier);
					reInstantiate = true;
				}
				try{
	        		if (analyzer.Analyze()) 
					{
						if(PlayerPrefs.GetInt ("way", 0) == 0){
							Screen.orientation = ScreenOrientation.Landscape;
						} else if (PlayerPrefs.GetInt ("way", 0) == 1){
							Screen.orientation = ScreenOrientation.Portrait;	
						}
						Destroy(menu);
						health.SetActive(true);
						coins.SetActive(true);
						Track track = new Track(trackWidth, trackHeight, trackDepth, Color.black, analyzer.Thresholds);
              	 	 	GetComponent<Camera>().transform.position = new Vector3(-trackWidth / 2, (float)(analyzer.Thresholds[0] * trackDepth + 100), 0);
						player.transform.position = new Vector3(-trackWidth / 2, (float)(analyzer.Thresholds[0] * trackDepth + 100), 0);
	          	  		isAnalyzed = true;
	        		}
				} catch(NullReferenceException){
					
				}
			}
	    }

		double diff;
		void UpdateCamera()
		{
			double d = -((playerZ + cameraDistanceZ) / trackHeight);
			int c = (int)d;

			if (c > 0)
			{
				diff = analyzer.Thresholds[c + 1] - analyzer.Thresholds[c];

				GetComponent<Camera>().transform.position = new Vector3(
					-trackWidth / 2, 
					(float)((analyzer.Thresholds[c] +  (d - c) * diff) * trackDepth + cameraDistanceY), 
					-(float)(playerZ + cameraDistanceZ)
					);
			}
		}
			
		float xPos = -512f;
		KeyCode left = KeyCode.A; KeyCode right = KeyCode.D;

		void UpdatePlayer(){
			double dPlayer = -(playerZ/trackHeight);
			int e = (int) dPlayer;

			if (e>0){
				if (player.transform.position.x == -250){
					if(Touch3D.left || Input.GetKeyDown(left)){
						xPos = -512f;
						Touch3D.left = false;
					}
				}
				if(transform.position.x == -512 ){
					if(Touch3D.left || Input.GetKeyDown(left)){
						xPos = -770f;
						Touch3D.left = false;
					}
				}
				if (player.transform.position.x == -512){
					if(Touch3D.right || Input.GetKeyDown(right)){
						xPos = -250f;
						Touch3D.right = false;
					}
				}
				if (player.transform.position.x == -770){
					if(Touch3D.right || Input.GetKeyDown(right)){
						xPos = -512f;
						Touch3D.right = false;
					}
				}
				player.transform.position = new Vector3(
					xPos, 
					(float)((analyzer.Thresholds[e] +  (dPlayer - e) * diff) * trackDepth + 125), 
					-(float)(playerZ + 100)
				);
			}
		}
		//double prev;

		void UpdateTarget()
		{
			int c = (int)(audio.timeSamples / sampleSize);

			if (c > lastSample && lastSample < analyzer.Thresholds.Length)
			{
				try{
				for (int i = lastSample + 1; i <= c; i++)
					
					targetPlayerZ += -analyzer.Thresholds[i] * trackHeight * analyzer.SpeedFactor;
					lastSample = c;
				} catch(IndexOutOfRangeException){
					Screen.orientation = ScreenOrientation.Portrait;	
					int timeInt = (int)time;
					PlayerPrefs.SetInt ("time", timeInt);
					PlayerPrefs.SetInt ("WIN", 1);
					SceneManager.LoadScene ("GameOver3DE");
				}
			}

		}
	}
}