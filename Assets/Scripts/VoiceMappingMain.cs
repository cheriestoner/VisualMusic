using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

public class VoiceMappingMain : MonoBehaviour {
    public float breakInterval; // indicate that the singing pauses
    [ReadOnly] public bool isSinging = false;
    [ReadOnly] public bool pitchDetected = false;
    [ReadOnly] public float spectralCentroid;
    [ReadOnly] public float attackTime;
    [ReadOnly] public float tempo;
    public OSC osc;
    [ReadOnly] public float pitchOSC;
    [ReadOnly] public float attackTimeOSC;
    [ReadOnly] public float tempoOSC;
    
    public LibPdInstance pdPatch;
    public ParticleSystem m_system;
    public ParticleSystemRenderer psRenderer;
    public Mesh[] meshes;
    
    public Toggle m_recorder;
    public TMPro.TMP_Text text;

    public int bufferSize; Methods.VisualizerBuffer buffer;
    public int tempoWindowSize; Methods.VisualizerBuffer beatBuffer;
    
    float lastOnset; // time of last onset
    int onsetCount;
    ParticleSystem.Particle[] m_particles;
    int stageNumber;
    bool visualizerRunning = false;
    bool tempoTracking = false;
    bool isRecording = false;
    bool inTransition = false;

    private Camera cameraRef;
    private static string dataFilePath;

    void Awake()
    {
        cameraRef = Camera.main;
        dataFilePath = Path.Combine(Application.persistentDataPath, "AudioVisualGameData");
    }

    // Start is called before the first frame update
    void Start()
    {
        pdPatch.Bind("Onset");
        osc.SetAddressHandler("/attack", OnReceiveAttack);
        osc.SetAddressHandler("/tempo", OnReceiveTempo);
        osc.SetAddressHandler("/pitch", OnReceivePitch);
        InitPSSettings(); 
        
        onsetCount = 0;
        stageNumber = 0;
        breakInterval = 5f;
        bufferSize = 120;
        tempoWindowSize = 5;
        buffer = new Methods.VisualizerBuffer("onsetLevel", bufferSize);
        beatBuffer = new Methods.VisualizerBuffer("tempo", tempoWindowSize);
    }

    void OnReceiveAttack(OscMessage message)
    {
		attackTimeOSC = message.GetFloat(2);
        text.text = "OSC test " + attackTimeOSC.ToString();
	}

    void OnReceiveTempo(OscMessage message)
    {
        tempoOSC = message.GetFloat(0);
        // StartCoroutine(ColorTransition)
    }

    void OnReceivePitch(OscMessage message)
    {
        pitchOSC = message.GetFloat(0);
    }

    void OnDestroy()
    {
        pdPatch.UnBind("Onset");
    }

    void InitPSSettings()
    {
        cameraRef.backgroundColor = Color.HSVToRGB(0, 0, 0.5f);

        var main = m_system.main;
        main.duration = 1f;
        main.startLifetime = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startDelay = 0;
        main.simulationSpeed = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        if(stageNumber == 0) main.startColor = Color.black;
        else main.startColor = Color.white;
        main.startSize = 1f;
        main.startSpeed = 1f;
        // m_system.randomSeed = 10;
        var emission = m_system.emission;
        emission.enabled = true;
        emission.rateOverTime = 50f;
        // initiate bursts
        // emission.SetBursts(
        //     new ParticleSystem.Burst[] {
        //         // new ParticleSystem.Burst(0.01f, 30, 1)
        //     }); 
        var shape = m_system.shape; shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.1f;
        var col = m_system.colorOverLifetime; col.enabled = false; col.color = Color.white;
        var vel = m_system.velocityOverLifetime; vel.enabled = true; vel.radial = 1f;
        ParticleSystem.MinMaxCurve speedCurve = new ParticleSystem.MinMaxCurve(1.0f, Methods.GetCurve(1f, 0f));
        vel.speedModifier = speedCurve;
        var size = m_system.sizeOverLifetime; size.enabled = false;
        var trails = m_system.trails; trails.enabled = false; trails.ratio = 1f;
        // var psr = GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        psRenderer.mesh = meshes[2];
        psRenderer.trailMaterial = psRenderer.material; //new Material(Shader.Find("Sprites/Default"));
    }

    public void StageZero()
    {
        stageNumber = 0;
        ResetVisualizer();
    }

    public void StageOne()
    {
        stageNumber = 1;
        ResetVisualizer();
    }

    public void StageTwo()
    {
        stageNumber = 2;
        ResetVisualizer();
    }

    void updateColor(float hue, float brightness)
    {
        var color = m_system.colorOverLifetime;
        var main = m_system.main;
        float h, s, v;
        Color.RGBToHSV(main.startColor.color, out h, out s, out v);
        color.color = Color.HSVToRGB(hue, 1, brightness);
        color.enabled = true;
    }

    void updateParticles(float hue, float brightness)
    {
        if (m_particles == null || m_particles.Length < m_system.main.maxParticles)
            m_particles = new ParticleSystem.Particle[m_system.main.maxParticles];
        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        int numParticlesAlive = m_system.GetParticles(m_particles);

        // Change only the particles that are alive 
        for (int i = 0; i < numParticlesAlive; i++)
        {
            float h, s, v;
            Color.RGBToHSV(m_particles[i].startColor, out h, out s, out v);
            m_particles[i].startColor = Color.HSVToRGB(hue, s, brightness);
            var main = m_system.main;
            main.startColor = Color.HSVToRGB(hue, s, brightness);
        }

        // Apply the particle changes to the Particle System
        m_system.SetParticles(m_particles, numParticlesAlive);
    }

    public void ResetVisualizer()
    {
        if(m_system.isPlaying) {
            m_system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        onsetCount = 0;
        buffer.Clear();
        beatBuffer.Clear();
        InitPSSettings();
        if(visualizerRunning) {
            StopCoroutine("ParticleVisualizer");
            visualizerRunning = false;
        }
        if(tempoTracking) {
            StopCoroutine("TempoTracker");
            tempoTracking = false;
        }
        // if(isRecording) {
        //     StopCoroutine("SaveStreamToFile");
        //     isRecording = false;
        // }
        if(m_recorder.isOn) {
            m_recorder.isOn = false;
        }
    }

    public void ListReceive(string sender, object[] list)
	{
		// This function will get called for *every* Float event sent by our
		// patch, so we need to make sure we're only acting on the
		// *AmplitudeEnvelope* event that we're actually interested in.
		if(sender == "Onset") // && GetComponent<OnsetMappingMain>().onset)
		{
            if(!isSinging) {
                isSinging = true;
                // Debug.Log("Singing starts!");
            }
            
            // attackSoundLevel = (float)list[1];
            spectralCentroid = Mathf.Round(10 * (float)list[2]) / 10f;
            onsetCount += 1;
            float est = GetComponent<AttackTracker>().EstimateAttackTime(); // in millisecond
            float onsetInterval;
            if(onsetCount <= 1) {
                attackTime = est;
            } else {
                onsetInterval = Time.time - lastOnset;
                attackTime = (est < 1000f*onsetInterval) ? est : 1000f*onsetInterval;
            }
            lastOnset = Time.time;
  
            if(!visualizerRunning) StartCoroutine("ParticleVisualizer");
            if(stageNumber > 0 && !tempoTracking) StartCoroutine("TempoTracker");
		}
	}

    // Update is called once per frame
    void Update()
    {
        if(isSinging && (Time.time - lastOnset) > breakInterval && !pitchDetected)
        {
            isSinging = false;
            Debug.Log("Singing stops!");
            if(m_system.isPlaying) m_system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            onsetCount = 0;
            if(beatBuffer.GetCount() > 0) beatBuffer.Clear();
        }
    }

    IEnumerator ParticleVisualizer() //float energy, float brightness
    {
        visualizerRunning = true;
        while(isSinging)
        {
            if(pitchDetected && !m_system.isPlaying) m_system.Play();
            // if(!pitchDetected) m_system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            float pitchScale, soundLevelScale, attackScale, centroidScale, shapeScale;
            pitchScale = 1f - Methods.LogMap(Mathf.Round(GetComponent<PitchAmplitude>().notePitch), 45f, 80f);
            soundLevelScale = Methods.LinearMap(GetComponent<PitchAmplitude>().soundLevel, 60f, 88f);
            attackScale = Methods.LinearMap(attackTimeOSC, 20f, 70f);
            centroidScale = Methods.LinearMap(Mathf.Round(10f*spectralCentroid)/10f, 3.0f, 3.9f);
            
            if(stageNumber == 0) {
                attackScale = 1;
            }

            if(stageNumber == 2) {
                pitchScale = 1 - pitchScale;
                soundLevelScale = 1 - soundLevelScale;
                attackScale = 1 - attackScale;
            }

            shapeScale = pitchScale * soundLevelScale;// * attackScale;

            var size = m_system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.1f, 10f, shapeScale), Methods.GetCurve(1f, 1f));
            if(stageNumber == 0) {
                yield return null;
                continue;
            }
            
            var main = m_system.main;
            main.startColor = Color.HSVToRGB(0, 0, centroidScale);
            var emission = m_system.emission;
            emission.rateOverTime = Mathf.Lerp(20, 50, 1 - shapeScale);

            var shape = m_system.shape;
            shape.radius = Mathf.Lerp(0.1f, 5f, 1 - shapeScale);
            psRenderer.mesh = meshes[Methods.FloorToIntSemiOpen(attackScale*3, 3)];

            // var vel = m_system.velocityOverLifetime;
            // vel.enabled = true;
            // vel.radial = Mathf.Lerp(0.1f, 5f, 1 - shapeScale);
            // var trails = m_system.trails;
            // float s = Mathf.Lerp(0.1f, 10f, shapeScale); // size constant
            // if(s > 1f) trails.enabled = false;
            // else {
            //     trails.enabled = true;
            //     if(s < 0.5f) trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f, Methods.GetCurve(1f, 0.5f/s));
            //     else trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f, Methods.GetCurve(1f, 0f));
            // }

            // buffer.MovingAverageStreaming(shapeScale);
            // if(!inTransition) {
            //     float h, s, v;
            //     Color.RGBToHSV(cameraRef.backgroundColor, out h, out s, out v);
            //     cameraRef.backgroundColor = Color.HSVToRGB(h, soundLevelScale, v);
            // }

            yield return null;
        }
        visualizerRunning = false;
    }

    IEnumerator BeatTracker()
    {
        int count = onsetCount;
        yield return new WaitForSeconds(1);
        Debug.Log("beats" + (onsetCount - count));
        beatBuffer.MovingAverageStreamingWithHop(onsetCount - count, tempoWindowSize - 1);
    }

    IEnumerator TempoTracker()
    {
        tempoTracking = true;
        while(isSinging)
        {
            yield return StartCoroutine("BeatTracker");

            if(beatBuffer.GetMovingAverage() >= 0) {
                tempo = Mathf.Round(beatBuffer.GetMovingAverage()); // beats per sec
                // color background
                int colorIndex;
                if(tempo <= 1) colorIndex = 1;
                else if(tempo >= 5) colorIndex = 5;
                else colorIndex = (int)tempo;
                StartCoroutine(ColorTransition(Methods.keyColors[colorIndex - 1], 0.5f));
            }
        }
        tempoTracking = false;
    }

    IEnumerator ColorTransition(Color endValue, float duration)
    {
        inTransition = true;
        float time = 0;
        Color startValue = cameraRef.backgroundColor;
        while (time < duration)
        {
            cameraRef.backgroundColor = Color.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cameraRef.backgroundColor = endValue;
        inTransition = false;
    }

    public void SaveStreamToFile()
    {   
        if(isRecording) {
            StopCoroutine("SaveStreamToFile");
            isRecording = false;
            Debug.Log("Data recording stops!");
        } else {
            StartCoroutine("SaveStreamToFile");
            Debug.Log("Data recording starts!" + dataFilePath);
        }
    }

    IEnumerator StreamRecorder()
    {
        isRecording = true;
        float timestamp = Time.time;
        while(true) {
            using(StreamWriter writer = new StreamWriter(dataFilePath + "_" + Methods.TimeToString(timestamp) + ".txt"))
            {
                // This will convert our Data object into a string of JSON
                // string dataToWrite = JsonUtility.ToJson(gameData);

                // This is where we actually write to the file
                // writer.Write(dataToWrite);
                writer.WriteLine("{0} {1} {2} {3} {4}", GetComponent<PitchAmplitude>().notePitch, GetComponent<PitchAmplitude>().soundLevel, spectralCentroid, attackTimeOSC, tempo);
            }
            yield return null;
        }
        // isRecording = false;
    }
}
