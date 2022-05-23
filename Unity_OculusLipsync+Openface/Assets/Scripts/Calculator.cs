using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Calculation such as Action Unit, gaze, and more.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Calculate moving average for each action unit.
    /// </summary>
    /// <param name="queue">The queue to be calculated</param>
    /// <param name="auNum">The number of Action Units</param>
    /// <returns>the list of data for each moving average of blendshape</returns>
    public float[] CalcMovingAverage(Queue<AnimationDataFrame> queue, int auNum)
    {
        float[] auValue = new float[auNum];
        // the status of queue won't be changed when using foreach statement
        foreach (var frame in queue)
        {
            // for each index of Action Unit
            for (int i = 0; i < auNum; i++)
            {
                // store all blendshape data for `queue.Length` times 
                auValue[i] += frame.d[i];
            }
        }

        // calc and store average for each au
        float[] blendshapeMovAve = new float[auNum];
        for (int i = 0; i < auNum; i++)
        {
            blendshapeMovAve[i] = auValue[i] / queue.Count;
        }

        return blendshapeMovAve;
    }

    public void CalcBlendshapeValue(ref HashSet<int> overallBlendshapes,
        ref Dictionary<int, float> curFrameBlendshapeVals,
        AnimationDataFrame frameDataOpenFace, AudioDataFrame frameDataAudio,
        SerializedBlendshapeMapping[] mappedBlendshapes,
        float[] blendshapeMovingAverage, Dictionary<string, int> blendDictStringToInt,
        bool doModulate)
    {
        foreach (var blend in overallBlendshapes)
        {
            curFrameBlendshapeVals[blend] = 0f;
        }

        // Take care of the blendshape frames
        for (int i = 12; i < frameDataOpenFace.d.Length; i++)
        {
            var mapping = mappedBlendshapes[i - 12];
            if (frameDataOpenFace.d[i] > 0 && blendshapeMovingAverage != null)
            {
                foreach (var blendshape in mapping.weightedBlendshapes)
                {
                    // todo: separate blendshapes to apply moving average or not ones
                    float val = .0f;
                    if (doModulate)
                        // val = ((frameData.d[i] / 5.0f) * 100f) * blendshape.weight;

                        val = ((blendshapeMovingAverage[i] / 6.0f) * 100f) * blendshape.weight;

                    // check if val is greater than predefined threshold
                    if (val >= mapping.threshold)
                    {
                        // if greater
                        if (curFrameBlendshapeVals.ContainsKey(blendDictStringToInt[blendshape.targetBlendshape]))
                        {
                            curFrameBlendshapeVals[blendDictStringToInt[blendshape.targetBlendshape]] += val;
                        }
                        else
                        {
                            curFrameBlendshapeVals[blendDictStringToInt[blendshape.targetBlendshape]] = val;
                        }
                    }
                    else
                    {
                        // otherwise, set target blendshape to 0
                        curFrameBlendshapeVals[blendDictStringToInt[blendshape.targetBlendshape]] = 0f;
                    }

                    if (!overallBlendshapes.Contains(blendDictStringToInt[blendshape.targetBlendshape]))
                    {
                        overallBlendshapes.Add(blendDictStringToInt[blendshape.targetBlendshape]);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Calculate the amount of modulation for each blendshapes based on audio features.
    /// </summary>
    public float CalcModulatedBlendshape()
    {
        return .0f;
    }
}