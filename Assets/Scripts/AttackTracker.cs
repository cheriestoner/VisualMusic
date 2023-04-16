using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AttackTracker : MonoBehaviour
{
    // [ReadOnly] public float attackTime;
    public LibPdInstance pdPatch;
    public ParticleSystem m_system;
    
    List<float> attackBuffer = new List<float>(); // calculate attack time
    const int attackBufferSize = 2048;
    int attackBufferPointer = 0;
    int attackStarts = -1;

    // Start is called before the first frame update
    void Start()
    {
        //Bind to the named send object.
		pdPatch.Bind("RawEnv");
    }

    void OnDestroy()
	{
		//Unbind from the named send object.
		pdPatch.UnBind("RawEnv");
	}
    
    // void AttackDetectorBufferLoader(float amp, ref List<float> buffer, ref int pointer, int bufferSize)
    // {
    //     if(buffer.Count < bufferSize) buffer.Add(amp);
    //     else buffer[pointer] = amp;
    //     if(amp < 10e-3) attackStarts = pointer; // Mathf.Epsilon?
    //     pointer = (pointer + 1) % bufferSize;
    // }

    void AttackDetectorBufferLoader(float amp)
    {
        if(attackBuffer.Count < attackBufferSize) attackBuffer.Add(amp);
        else attackBuffer[attackBufferPointer] = amp;
        if(amp < 10e-3) attackStarts = attackBufferPointer; // Mathf.Epsilon?
        attackBufferPointer = (attackBufferPointer + 1) % attackBufferSize;
    }

    // public float EstimateAttackTime(ref List<float> buffer, int bufferSize, int attackStarts)
    // {
    //     // Debug.LogFormat("Attack detector buffer size {0}, pointer {1}", buffer.Count, attackBufferPointer);
    //     // attack time in samples
    //     int peakId = buffer.IndexOf(buffer.Max());
    //     // int lastZero; // search back for consecutive zero values
    //     // for(int i = peakId; ; i--）
    //     // {
    //     //     if(buffer[i] < 1e-4)
    //     // }
    //     // Debug.LogFormat("Peak value {0} at {1}", peak, peakId);
    //     if(attackStarts > 0) 
    //         return (peakId - attackStarts + bufferSize) % bufferSize;
    //     else return Mathf.Infinity;
    // }

    int LocalMaximumBackwards(List<float> values, int startPos)
    {
        int size = values.Count;
        // Debug.LogFormat("buffer size {0}, current pos {1}", values.Count, startPos);
        // check the latest point
        if(values[startPos] > values[(startPos - 1 + size) % size]) return startPos;
        else {
            int pos = startPos;
            for(int i = size-1; i > 0; i--) {
                if(values[(pos - 1 + size) % size] > values[pos] && values[(pos - 1 + size) % size] > values[(pos - 2 + size) % size]) {
                    return (pos - 1 + size) % size;
                }
                pos = (pos - 1 + size) % size;
            }
            for(int i = size-1; i > 0; i--) {
                if((values[(pos - 1 + size) % size] >= values[pos] && values[(pos - 1 + size) % size] > values[(pos - 2 + size) % size]) 
                || (values[(pos - 1 + size) % size] > values[pos] && values[(pos - 1 + size) % size] >= values[(pos - 2 + size) % size])) {
                    return (pos - 1 + size) % size;
                }
                pos = (pos - 1 + size) % size;
            }
            return startPos; // when all elements are equal
        }
    }

    int LocalMinimumBackwards(List<float> values, int startPos)
    {
        int size = values.Count;
        // check the oldest point
        if(values[(startPos + 1) % size] < values[(startPos + 2) % size]) return (startPos + 1) % size; 
        else {
            int pos = startPos;
            for(int i = size-1; i > 0; i--) {
                if(values[(pos - 1 + size) % size] < values[pos] && values[(pos - 1 + size) % size] < values[(pos - 2 + size) % size]) {
                    return (pos - 1 + size) % size;
                }
                pos = (pos - 1 + size) % size;
            }
            for(int i = size-1; i > 0; i--) {
                if((values[(pos - 1 + size) % size] <= values[pos] && values[(pos - 1 + size) % size] < values[(pos - 2 + size) % size]) 
                || (values[(pos - 1 + size) % size] < values[pos] && values[(pos - 1 + size) % size] <= values[(pos - 2 + size) % size])) {
                    return (pos - 1 + size) % size;
                }
                pos = (pos - 1 + size) % size;
            }
            return startPos; // when all elements are equal
        }
    }

    public float EstimateAttackTime()
    {
        // attack time: samples -> milliseconds

        // int peakId = attackBuffer.IndexOf(attackBuffer.Max());
        // if(attackStarts >= 0) 
        // {
        //     int t = (peakId - attackStarts + attackBufferSize) % attackBufferSize;
        //     if(t == 0) t = attackBufferSize;
        //     return t;
        // }
        // else return Mathf.Infinity;

        List<float> values = attackBuffer;
        int pos = (attackBufferPointer - 1 + attackBufferSize) % attackBufferSize;
        int t = LocalMaximumBackwards(values, pos) - attackStarts;// - LocalMinimumBackwards(values, pos);
        t = (t + attackBufferSize) % attackBufferSize;
        if(t == 0) t = attackBufferSize;
        return 1000f * ((float)t / GetComponent<AudioInput>().audioSampleRate); // in millisecond
    }

    public void FloatReceive(string sender, float value)
    {
        // raw linear amplitude envelope for calculating attack speed
        if(sender == "RawEnv")
        {
            AttackDetectorBufferLoader(value);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
