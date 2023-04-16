using System.Collections;
using System.Collections.Generic; // So we can use List<>
using UnityEngine;
using UnityEngine.UI;

public class ReadOnlyAttribute : PropertyAttribute { } // Custom drawer in Editor

[RequireComponent(typeof(AudioSource))]
public class AudioInput : MonoBehaviour
{
    [ReadOnly] public string microphone;
	public TMPro.TMP_Dropdown micDropdown;
	public Toggle micToggle;
	public Button resetButton;
    // public float minThreshold = 0;
	// public float frequency = 0.0f;
	public int audioSampleRate = 44100;
	
    // public FFTWindow fftWindow;
    // private int samples = 8192; // for audio analysis: get fundamental freq.

	private List<string> options = new List<string>();
    private AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // get all available microphones
		foreach (string device in Microphone.devices) 
		{
			if (string.IsNullOrEmpty(microphone)) // (microphone == null) didn't work for no reason
			{  
				//set default mic to first mic found.
				microphone = device;
				Debug.Log("Microphone detected: " + microphone);
			}
			options.Add(device);
		}
		microphone = options[PlayerPrefsManager.GetMicrophone()];

		//add mics to dropdown
		micDropdown.AddOptions(options);
		micDropdown.onValueChanged.AddListener(delegate {
		micDropdownValueChangedHandler(micDropdown);
		});	

        //initialize input with default mic
		UpdateMicrophone ();

		micToggle.onValueChanged.AddListener(delegate {
			toggleValueChanged(micToggle);
		});
    }

    void UpdateMicrophone()
	{
		audioSource.Stop(); 
		//Start recording to audioclip from the mic
		audioSource.clip = Microphone.Start(microphone, true, 10, audioSampleRate);
		audioSource.loop = true; 
		// Mute the sound with an Audio Mixer group because we don't want the player to hear it
		Debug.Log("Microphone recording status: " + Microphone.IsRecording(microphone).ToString());

		if (Microphone.IsRecording(microphone)) { //check that the mic is recording, otherwise you'll get stuck in an infinite loop waiting for it to start
			while (!(Microphone.GetPosition(microphone) > 0)) {
				// Debug.Log("Waiting for audio input");
			} // Wait until the recording has started. 
		
			Debug.Log("Recording started with " + microphone);

			// Start playing the audio source
			audioSource.Play(); 
		} else {
			//microphone doesn't work for some reason

			Debug.Log(microphone + " doesn't work!");
		}
	}

	public void toggleValueChanged(Toggle tg)
	{
		if(tg.isOn) {
			// microphone = options[mic.value];
			Debug.Log("Listening...");
			UpdateMicrophone();
			resetButton.onClick.Invoke();
		} else {
			audioSource.Stop();
			Debug.Log("Mic muted!");
			resetButton.onClick.Invoke();
		}
	}

	public void micDropdownValueChangedHandler(TMPro.TMP_Dropdown mic){
		microphone = options[mic.value];
		UpdateMicrophone();
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
