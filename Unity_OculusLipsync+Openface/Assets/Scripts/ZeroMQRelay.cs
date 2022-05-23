using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.PlayerLoop;

/// <summary>
/// TODO: Figure out how we're going to figure out what the sources of data are actually from. 
/// Currently, I think this will be done by looking at the array length
/// </summary>
public class ZeroMQRelay : MonoBehaviour
{
    public static Action<AnimationDataFrame> OpenFaceDataReceived;
    public static Action<AudioDataFrame> AudioDataReceived;

    public static string[] OpenFaceDataColumns =
    {
        "timestamp",
        "head_pos_x",
        "head_pos_y",
        "head_pos_z",
        "head_x",
        "head_y",
        "head_z",
        "left_eye_x",
        "left_eye_y",
        "left_eye_z",
        "right_eye_x",
        "right_eye_y",
        "right_eye_z",
        "AU01_InnerBrowRaiser",
        "AU02_OuterBrowRaiser",
        "AU04_BrowLowerer",
        "AU05_UpperLidRaiser",
        "AU06_CheekRaiser",
        "AU07_LidTightener",
        "AU09_NoseWrinkler",
        "AU10_UpperLipRaiser",
        "AU12_LipCornerPuller",
        "AU14_Dimpler",
        "AU15_LipCornerDepressor",
        "AU17_ChinRaiser",
        "AU20_LipStretcher",
        "AU22_LipTightener",
        "AU25_LipsPart",
        "AU26_JawDrop",
        "AU45_Blink"
    };


    private void OnEnable()
    {
        // new delegation is added here
        ZeroMQReceiver.NewZeroMQMessageReceivedEventOpenFace += OnNewMessageReceivedOpenFace;
        ZeroMQReceiver.NewZeroMQMessageReceivedEventAudio += OnNewMessageReceivedAudio;
    }

    private void OnDisable()
    {
        ZeroMQReceiver.NewZeroMQMessageReceivedEventOpenFace -= OnNewMessageReceivedOpenFace;
        ZeroMQReceiver.NewZeroMQMessageReceivedEventAudio -= OnNewMessageReceivedAudio;
    }

    private void OnNewMessageReceivedOpenFace(List<string> messages)
    {
        AnimationDataFrame frame;
        foreach (string message in messages)
        {
            frame = DeserializeStringOpenFace(message);
            if (frame.d.Length == (OpenFaceDataColumns.Length - 1))
            {
                OpenFaceDataReceived?.Invoke(frame);
            }
        }
    }

    private void OnNewMessageReceivedAudio(List<string> messages)
    {
        AudioDataFrame frame;
        foreach (string message in messages)
        {
            frame = DeserializeStringAudio(message);
            AudioDataReceived?.Invoke(frame);
        }
    }

    private AnimationDataFrame DeserializeStringOpenFace(string message)
    {
        var unescaped = Regex.Unescape(message);
        AnimationDataFrame paf = JsonConvert.DeserializeObject<AnimationDataFrame>(unescaped);
        return paf;
    }

    private AudioDataFrame DeserializeStringAudio(string message)
    {
        AudioDataFrame paf = JsonConvert.DeserializeObject<AudioDataFrame>(message);
        return paf;
    }
}

[Serializable]
public struct AnimationDataFrame
{
    public float t;
    public float[] d;
}

[Serializable]
public struct AudioDataFrame
{
    public float t;
    public int totalVoicedRegionNum;
    public float totalVoicedTimeMs;
    public float averageRms;
    public float averageRmsDb;
    public float averageRmsDbTotal;
    public float stdRmsDb;
    public float stdRmsDbTotal;
    public float averageF0;
    public float stdF0;
}