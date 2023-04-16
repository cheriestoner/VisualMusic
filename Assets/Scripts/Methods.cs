using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Methods : MonoBehaviour
// useful static methods
{
    const int WINDOW = 10;

    public static Vector2 tempoRange = new Vector2(90f, 220f);
    public static Vector2 vSLRange = new Vector2(3.4f, 6f);
    public static Vector2 vF0Range = new Vector2(2f, 4f);
    public static Vector2 centroidRange = new Vector2(2.8f, 4f);

    public static Color[] keyColors = {Color.HSVToRGB(0.75f, 0.6f, 0.6f), Color.HSVToRGB(0.667f, 0.6f, 0.6f), Color.HSVToRGB(0.92f, 0.4f, 1), Color.HSVToRGB(0.105f, 0.8f, 1), Color.HSVToRGB(0, 1, 0.78f)};

    public struct VisualizerBuffer 
    {
        public string name;
        public int windowSize;// = 20;
        public List<float> buffer;// = new List<float>();
        public int pointer;// = 0;
        public int zeroPointer;// = -1;
        public float sum;// = 0;
        public float mavg;

        public VisualizerBuffer(string name, int windowSize) {
            this.name = name;
            this.windowSize = windowSize;
            this.buffer = new List<float>();
            this.pointer = 0;
            this.zeroPointer = -1;
            this.sum = 0;
            this.mavg = -1;
        }

        public void MovingAverageStreaming(float value) {
            sum += value;
            if(buffer.Count < windowSize) {
                buffer.Add(value);
                if(buffer.Count == windowSize) mavg = sum / windowSize;
            } else {
                sum -= buffer[pointer];
                buffer[pointer] = value;
                mavg = sum / windowSize;
            }
            pointer = (pointer + 1) % windowSize; // note that % is not quivalent to modulo in C# but works when the dividend is positive
        }

        public void MovingAverageStreamingWithHop(float value, int hopsize=1)
        {
            Debug.LogFormat("Status: length {0}, size {1}", buffer.Count, windowSize);
            if(hopsize > windowSize - 1) hopsize = windowSize - 1;
            sum += value;
            if(buffer.Count < windowSize) {
                buffer.Add(value);
                if(buffer.Count == windowSize) mavg = sum / windowSize;
            } else {
                sum -= buffer[pointer];
                buffer[pointer] = value;
                if(hopsize == 1) mavg = sum / windowSize;
                else if((pointer + 1) % hopsize == hopsize - 1) mavg = sum / windowSize;
            }
            pointer = (pointer + 1) % windowSize; // note that % is not quivalent to modulo in C# but works when the dividend is positive
        }

        public float GetMovingAverage() {
            return mavg;
        }

        public float GetVariance() {
            // only when buffer.Count == windowSize
            if(buffer.Count < windowSize) return -1;
            float v = 0;
            foreach(float value in buffer) {
                v += Mathf.Pow(mavg - value, 2);
            }
            v = v / (windowSize - 1); // unbiased sample variance
            return v;
        }

        public int GetCount() {
            return buffer.Count;
        }

        public void Clear() {
            buffer.Clear();
            sum = 0;
            mavg = 0;
            zeroPointer = -1;
            pointer = 0;
        }
    }

    public static AnimationCurve GetCurve(float start, float end, float tangent = 0f)
    {
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, start); keys[0].inTangent = tangent;
        keys[1] = new Keyframe(1, end); keys[1].inTangent = tangent;
        AnimationCurve curve = new AnimationCurve(keys);
        return curve;
    }

    public static string TimeToString(float time)
    {
        int min = (int)time / 60;
        int sec = (int)time % 60;
        return min.ToString() + sec.ToString();
    }

    public static float DistToCamera(float objPos=0f)
    {
        // objPos: the closest position to the camera, 0 by default
        return Mathf.Abs(objPos - Camera.main.transform.position.x); // on the x-axis
    }

    public static float FovSize(float dist)
    {
        float fov = Camera.main.fieldOfView;
        return Mathf.Tan(fov * Mathf.Deg2Rad / 2f) * dist * 2f;
    }

    public static float LinearMap(float value, float min, float max)
    {
        // to 0 - 1
        if(value < min) value = min;
        if(value > max) value = max;
        return (value - min) / (max - min);
    }

    public static float LogMap(float value, float min, float max)
    {
        // equivalent to linear map of log(value)
        // to 0 - 1
        if(value < min) value = min;
        if(value > max) value = max;
        return Mathf.Log(value/min, 10) / Mathf.Log(max/min, 10);
    }

    public static int FloorToIntSemiOpen(float value, float sup)
    {
        if((sup - value) <= Mathf.Epsilon) return Mathf.RoundToInt(value) - 1;
        else return Mathf.FloorToInt(value);
    }

    public static void MovingAverageStreaming(float value, ref List<float> values, ref float mavg, ref float sum, ref int pointer, int window=4)
    {
        // Moving Average
        // List<float> values is maintained as a ring buffer
        // The buffer pointer always points to the latest element of the buffer, 
        // and the next pointer is pointed to the "start" of the buffer

        sum += value;
        if(values.Count < window) 
        {
            values.Add(value);
            if(values.Count == window) mavg = sum / window;
        } else {
            sum -= values[pointer];
            values[pointer] = value;
            // Debug.LogFormat("Sum {0}, pointer {1}, pointer value {2}, window size {3}", sum, pointer, values[pointer], window);
            mavg = sum / window;
        }
        pointer = (pointer + 1) % window; // note that % is not quivalent to modulo in C# but works when the dividend is positive
    }

    public static float GetVariance(float mean, List<float> values)
    {
        float v = 0;
        foreach(float value in values)
        {
            v += Mathf.Pow(mean - value, 2);
        }
        v = v / (values.Count - 1); // unbiased sample variance
        return v;
    }
}