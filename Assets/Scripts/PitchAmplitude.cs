using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchAmplitude : MonoBehaviour
{
    [ReadOnly] public float notePitch;
    [ReadOnly] public float pitchAvg;
    [ReadOnly] public float pitchVariability;
    [ReadOnly] public float soundLevel;
    public LibPdInstance pdPatch;
    public ParticleSystem m_system;

    ParticleSystem.Particle[] m_particles;
    public int pitchBufferSize = 50;
    Methods.VisualizerBuffer pitchBuffer;
    public bool silence = false;
    
    // Start is called before the first frame update
    void Start()
    {
        pdPatch.Bind("PitchAmp");
        pitchBuffer = new Methods.VisualizerBuffer("pitch", pitchBufferSize);
    }

    void OnDestroy()
	{
		//Unbind from the named send object.
		pdPatch.UnBind("PitchAmp");
        //pdPatch.unBind("OnsetBang");
	}

    public void ListReceive(string sender, object[] values)
	{
		//This function will get called for *every* Float event sent by our
		//patch, so we need to make sure we're only acting on the
		//*sender* event that we're actually interested in.

        // high pitch --> smaller object, brighter lighter color
        if(sender == "PitchAmp") // && GetComponent<OnsetMappingMain>().onset)
        {
            float pitch = Mathf.Round((float)values[0]);
            soundLevel = Mathf.Round(10f * (float)values[1]) / 10f;
            // if(!GetComponent<VoiceMappingMain>().isSinging && pitchBuffer.GetCount() > 0) pitchBuffer.Clear();
            if(!GetComponent<MappingOSC>().isSinging && pitchBuffer.GetCount() > 0) pitchBuffer.Clear();

            if(pitch < 40f) 
            {
                // GetComponent<VoiceMappingMain>().pitchDetected = false;
                GetComponent<MappingOSC>().pitchDetected = false;
                if(!silence) StartCoroutine("SilenceTimer");
                // soundLevel = 0f;
            } else {
                // GetComponent<VoiceMappingMain>().pitchDetected = true;
                GetComponent<MappingOSC>().pitchDetected = true;
                if(silence) StopCoroutine("SilenceTimer");
                pitch = Mathf.Round((float)values[0]);
                // pitch moving average
                pitchBuffer.MovingAverageStreaming(pitch);
                pitchAvg = pitchBuffer.GetMovingAverage();
                if(pitchBuffer.GetVariance() > 0) pitchVariability = Mathf.Sqrt(pitchBuffer.GetVariance());
            
                // if(pitchAvg > 0) notePitch = Mathf.Round(10f * pitchAvg) / 10f;
                // else notePitch = Mathf.Round(pitch*10f)/10f;
                notePitch = pitch;
            }
        }
    }

    IEnumerator SilenceTimer()
    {
        silence = true;
        yield return new WaitForSeconds(1);
        GetComponent<MappingOSC>().isSinging = false;
        silence = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
