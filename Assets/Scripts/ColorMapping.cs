using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorMapping : MonoBehaviour
{
    [ReadOnly] public float centroid;
    public LibPdInstance pdPatch;
    // public ParticleSystem m_system; 
    // Start is called before the first frame update
    float hue, saturation, brightness;
    Dictionary<string, float> emotionHue = new Dictionary<string, float>();
    Dictionary<string, string> emotionAcoustics = new Dictionary<string, string>();
    // Dictionary<string, Vector2> ranges = new Dictionary<string, Vector2>();
    Vector2 tempoRange, vSLRange, vF0Range, centroidRange;

    void Start()
    {
        pdPatch.Bind("SpecCentroid");
        InitializeColorSetting();
        InitializeEmotionCueSetting();
    }

    void InitializeColorSetting()
    {
        Color.RGBToHSV(RenderSettings.ambientLight, out hue, out saturation, out brightness);

        emotionHue.Add("000", 0.75f); /*sad or tender: violet*/
        emotionHue.Add("101", 0.167f); /*happy: yellow*/
        emotionHue.Add("111", 0f); /*angry: red*/
        emotionHue.Add("110", 0.667f); /*fearful: blue*/
    }

    void InitializeEmotionCueSetting()
    {
        emotionAcoustics.Add("000", "Sadness");
        emotionAcoustics.Add("101", "Happiness");
        emotionAcoustics.Add("111", "Anger");
        emotionAcoustics.Add("110", "Fear");

        tempoRange = new Vector2(90f, 220f);
        vSLRange = new Vector2(3.4f, 6f);
        vF0Range = new Vector2(2f, 4f);
        centroidRange = new Vector2(2.8f, 4f);
        // ranges.Add("tempo", new Vector2(90f, 220f));
        // ranges.Add("vSL", new Vector2(3.4f, 6f));
        // ranges.Add("vF0", new Vector2(2f, 4f));
        // ranges.Add("centroid", new Vector2(2.8f, 4f));
    }

    void OnDestroy()
	{
		//Unbind from the named send object.
		pdPatch.UnBind("SpecCentroid");
	}

    void AcousticMapping(float cue, ref Vector2 range, out float value, out string category)
    {
        // Vector2(min, max)
        // pitchVariability 2 - 4
        // soundLevelVariability 3.4 - 6
        // tempo 90 - 220
        //todo: decimal precision?
        if(cue < range.x) range.x = cue;
        if(cue > range.y) range.y = cue;
        value = (cue - range.x) / (range.y - range.x);
        category = System.Convert.ToInt32(value > 0.5f).ToString();
    }

    void EmotionColor(float tempo, float vSL, float vF0, out string h, out float s)
    {
        float v1, v2, v3;
        string c1, c2, c3;
        AcousticMapping(tempo, ref tempoRange, out v1, out c1);
        AcousticMapping(vSL, ref vSLRange, out v2, out c2);
        AcousticMapping(vF0, ref vF0Range, out v3, out c3);
        h = string.Concat(c1, c2, c3);
        s = 2f * Mathf.Abs((v1 + v2 + v3) / 3f - 0.5f);
    }

    // void AngerToColorHS(float attackLevel, out float h, out float s)
    // {
    //     // when the tempo is irregular, it is very likely to be angry
    //     float maxLv = 40;
    //     if(attackLevel > maxLv) maxLv = attackLevel;
    //     if(Mathf.Abs(attackLevel - maxLv)/maxLv < 0.2f)
    //     {
    //         h = emotionHue["angry"];
    //         s = 1 - 2 * Mathf.Abs(attackLevel - maxLv)/maxLv;
    //     } else {
    //         h = hue; s = saturation;
    //     }
    // }

    // float minCentroid = 2.8f, maxCentroid = 4.0f;
    public void FloatReceive(string sender, float value)
	{
        if(sender == "SpecCentroid")
        {
            if(value < centroidRange.x) centroidRange.x = value;
            if(value > centroidRange.y) centroidRange.y = value;
            brightness = (Mathf.Round(value*10)/10 - centroidRange.x) / (centroidRange.y - centroidRange.x);
            centroid = value;

            // if(value < minCentroid) minCentroid = value;
            // if(value > maxCentroid) maxCentroid = value;
            // brightness = (Mathf.Round(value*10)/10 - minCentroid) / (maxCentroid - minCentroid);
            // centroid = value;
        }
    }
    // Update is called once per frame
    void Update()
    {
        // todo: how to interpolate color in between in real time?
        // float tempo = (float)GetComponent<VoiceMappingMain>().tempoBPM, vSL = GetComponent<OnsetSPL>().attackSoundLevelVariability, vF0 = GetComponent<PitchMapping>().pitchVariability;
        // if(tempo > 0 && vSL > 0)
        // {
        //     string h;
        //     float s;
        //     // TempoToColorHS((float)GetComponent<VoiceMappingMain>().tempoBPM, out h, out s);
        //     EmotionColor(tempo, vSL, vF0, out h, out s);
        //     if(emotionHue.ContainsKey(h)) RenderSettings.ambientLight = Color.HSVToRGB(emotionHue[h], s, brightness);
        //     else RenderSettings.ambientLight = Color.HSVToRGB(hue, saturation, brightness);
        // } else {
        //     RenderSettings.ambientLight = Color.HSVToRGB(hue, saturation, brightness);
        // }

    }
}
