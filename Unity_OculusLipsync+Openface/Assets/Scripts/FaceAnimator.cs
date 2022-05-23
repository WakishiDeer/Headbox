﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class FaceAnimator : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMeshRenderer;

    public GameObject avatar, head_bone;
    private Mesh skinnedMesh;

    private Dictionary<string, int> blendDictStringToInt = new Dictionary<string, int>();
    private Dictionary<int, string> blendDictIntToString = new Dictionary<int, string>();
    private HashSet<int> overallBlendshapes;

    public float speakingThreshold = 10f;
    public float blendshapeInterpolationSpeed = 10f;
    public float headRotationSpeed = 10f;
    public float eyeRotationSpeed = 10f;

    public float headRotationMultiplier = .1f;
    int blendShapeCount = 0;

    private AnimationDataFrame frameDataOpenFace;
    private AudioDataFrame frameDataAudio;

    private int maxSizeQueue = 10;
    private Queue<AnimationDataFrame> actionUnitQueue = new Queue<AnimationDataFrame>(); // Store data for action unit 

    private Calculator calc = new Calculator();
    private float[] blendshapeMovingAverage;
    private bool hasNewDataUpdate = false;
    private bool hasNewBlendshapeVals = false;

    public InterpolationType BlendshapeInterpolationType = InterpolationType.EaseInOut;

    private Dictionary<int, float> curFrameBlendshapeVals = new Dictionary<int, float>();
    private Dictionary<int, float> targetFrameBlendshapeVals = new Dictionary<int, float>();

    [Tooltip("The JSON file that contains the input-to-blendshape blendshape mapping.")]
    public TextAsset BlendshapeMappingFile;

    public SerializedBlendshapeMapping[] mappedBlendshapes;

    void Awake()
    {
        // get MB / MH model
        skinnedMeshRenderer = avatar.GetComponent<SkinnedMeshRenderer>();
        skinnedMesh = avatar.GetComponent<SkinnedMeshRenderer>().sharedMesh;
    }

    // Use this for initialization
    void Start()
    {
        // create dict of all blendshapes this skinnedMesh has

        Debug.Log("Loading Blendshapes");
        blendShapeCount = skinnedMesh.blendShapeCount;
        for (int i = 0; i < blendShapeCount; i++)
        {
            string expression = skinnedMesh.GetBlendShapeName(i);
            blendDictStringToInt.Add(expression, i);
            blendDictIntToString.Add(i, expression);
        }

        var serializedMapping = JsonUtility.FromJson<SerializedBlendshapeMappingData>(BlendshapeMappingFile.ToString());
        mappedBlendshapes = serializedMapping.data;

        overallBlendshapes = new HashSet<int>();
        HookIntoZeroMQRelay();
        HookIntoAudioRelay();
    }

    private void Update()
    {
        if (hasNewDataUpdate)
        {
            curFrameBlendshapeVals = new Dictionary<int, float>();
            // Deal with the timestamps, head + eye positions and rots, etc.

            Vector3 headRot = new Vector3(frameDataOpenFace.d[3] * Mathf.Rad2Deg, // Represents Up/down in OpenFace
                frameDataOpenFace.d[4] * Mathf.Rad2Deg, // Turn
                frameDataOpenFace.d[5] * Mathf.Rad2Deg); // Tilt
            // Convert world corrdinate of `headRot` into local one, using `head_bone`
            Vector3 headRotLocal = head_bone.transform.InverseTransformDirection(headRot);
            head_bone.transform.localRotation = Quaternion.Slerp(head_bone.transform.localRotation,
                Quaternion.Euler(headRotLocal),
                Time.deltaTime * headRotationSpeed);

            bool modulationMode = false;
            if (modulationMode)
            {
                Debug.Log("pass this");
                // CalcModulatedBlendshape();
            }
            else
            {
                // `overallBlendshapes` and `curFrameBlendshapeVals` are passed by reference, because values are updated
                calc.CalcBlendshapeValue(overallBlendshapes: ref overallBlendshapes,
                    curFrameBlendshapeVals: ref curFrameBlendshapeVals,
                    frameDataOpenFace: frameDataOpenFace, frameDataAudio: frameDataAudio,
                    mappedBlendshapes: mappedBlendshapes, blendshapeMovingAverage: blendshapeMovingAverage,
                    blendDictStringToInt: blendDictStringToInt, doModulate: true);
            }

            hasNewDataUpdate = false;
            hasNewBlendshapeVals = true;
        }
    }

    private void UpdateActionUnitQueue(Queue<AnimationDataFrame> actionUnitQueue, int auNum)
    {
        // update blendshape average
        actionUnitQueue.Enqueue(frameDataOpenFace); // incomingFrame
        // check if data of queue is over 
        if (actionUnitQueue.Count > maxSizeQueue)
        {
            AnimationDataFrame outgoingFrame = actionUnitQueue.Dequeue();
            // calculate moving average to make avatar-movement smoothness
            blendshapeMovingAverage = calc.CalcMovingAverage(actionUnitQueue, auNum);
        }
    }

    public void LateUpdate()
    {
        if (hasNewDataUpdate)
        {
            targetFrameBlendshapeVals.Clear();
            foreach (var curFrameBlendShape in curFrameBlendshapeVals)
            {
                targetFrameBlendshapeVals.Add(curFrameBlendShape.Key, curFrameBlendShape.Value);
            }

            hasNewBlendshapeVals = false;
        }

        foreach (var curFrameBlendShape in curFrameBlendshapeVals)
        {
            float blendshapeVal = Interpolate(BlendshapeInterpolationType,
                skinnedMeshRenderer.GetBlendShapeWeight(curFrameBlendShape.Key), curFrameBlendShape.Value,
                Time.deltaTime * blendshapeInterpolationSpeed);
            skinnedMeshRenderer.SetBlendShapeWeight(curFrameBlendShape.Key, blendshapeVal);
        }
    }

    public void OnAnimationDataUpdated(AnimationDataFrame lastDataSet)
    {
        frameDataOpenFace = lastDataSet;
        UpdateActionUnitQueue(actionUnitQueue, frameDataOpenFace.d.Length);
        hasNewDataUpdate = true;
    }

    public void OnAudioDataUpdated(AudioDataFrame lastDataset)
    {
        frameDataAudio = lastDataset;
    }

    public void HookIntoZeroMQRelay()
    {
        ZeroMQRelay.OpenFaceDataReceived += OnAnimationDataUpdated;
    }

    public void UnhookFromZeroMQRelay()
    {
        ZeroMQRelay.OpenFaceDataReceived -= OnAnimationDataUpdated;
    }

    public void HookIntoAudioRelay()
    {
        ZeroMQRelay.AudioDataReceived += OnAudioDataUpdated;
    }

    public void UnhookIntoAudioRelay()
    {
        ZeroMQRelay.AudioDataReceived -= OnAudioDataUpdated;
    }

    private bool IsCurrentlySpeaking()
    {
        /* foreach (var viseme in LipSyncComponent.MappedVisemes)
         {
             foreach (var blendshape in viseme.WeightedVisemes)
             {
                 if (skinnedMeshRenderer.GetBlendShapeWeight(blendDictStringToInt[blendshape.targetBlendshape]) > speakingThreshold)
                 {
                     return true;
                 }
             }
         }*/

        //foreach (var blendshape in LipSyncComponent.LaughterBlendTargets)
        //{
        //    if (skinnedMeshRenderer.GetBlendShapeWeight(blendDictStringToInt[blendshape]) > speakingThreshold)
        //    {
        //        return true;
        //    }
        //}

        return false;
    }

    public enum InterpolationType
    {
        Linear = 0,
        EaseIn = 1,
        EaseOut = 2,
        EaseInOut = 3,
        Boing = 4,
        Hermite = 5,
    }

    /// <summary>
    /// Interpolates from a value to another value using a specified interpolation type.
    /// </summary>
    /// <param name="type">The type of interpolation to use. EaseInOut is recommended for most cases.</param>
    /// <param name="from">The value at the start of interpolation.</param>
    /// <param name="to">The value at the end of interpolation.</param>
    /// <param name="t">A value from 0 to 1 that determines how far along the interpolation is.</param>
    /// <returns>The interpolated value calculated from the input parameters.</returns>
    public static float Interpolate(InterpolationType type, float from, float to, float t)
    {
        switch (type)
        {
            case InterpolationType.Linear:
                return Mathf.Lerp(from, to, t);
            case InterpolationType.EaseIn:
                return Mathf.Lerp(from, to, 1.0f - Mathf.Cos(t * Mathf.PI * 0.5f));
            case InterpolationType.EaseOut:
                return Mathf.Lerp(from, to, Mathf.Sin(t * Mathf.PI * 0.5f));
            case InterpolationType.EaseInOut:
                return Mathf.SmoothStep(from, to, t);
            case InterpolationType.Boing:
                t = Mathf.Clamp01(t);
                t = (Mathf.Sin(t * Mathf.PI * (0.2f + 2.5f * t * t * t)) * Mathf.Pow(1f - t, 2.2f) + t) *
                    (1f + (1.2f * (1f - t)));
                return from + (to - from) * t;
            case InterpolationType.Hermite:
                return Mathf.Lerp(from, to, t * t * (3.0f - 2.0f * t));
            default:
                Debug.LogError("Invalid InterpolationType of: " + type.ToString());
                return from;
        }
    }
}

// Refer to the `OpenFace_Blendshape_Mapping.json`
[Serializable]
public class AffectedBlendshape
{
    public string targetBlendshape;
    public float weight;
}

[Serializable]
public class SerializedBlendshapeMapping
{
    public string inputName;
    public AffectedBlendshape[] weightedBlendshapes;
    public float threshold;
}

[Serializable]
public class SerializedBlendshapeMappingData
{
    public SerializedBlendshapeMapping[] data;
}


[Serializable]
public class SerializedVisemeMapping
{
    public string inputName;
    public AffectedBlendshape[] WeightedVisemes;
    public float threshold;
}

[Serializable]
public class SerializedVisemeMappingData
{
    public SerializedVisemeMapping[] data;
}