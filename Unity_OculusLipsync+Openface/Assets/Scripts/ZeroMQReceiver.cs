using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public abstract class ZeroMQReceiver : MonoBehaviour
{
    public static Action<List<string>> NewZeroMQMessageReceivedEventOpenFace;
    public static Action<List<string>> NewZeroMQMessageReceivedEventAudio;
    public bool startListeningOnAwake = true;
    protected string OriginIP = "127.0.0.1";
    protected string OriginPort;
    protected bool ListenerThreadCancelled;

    protected readonly ConcurrentQueue<List<string>> ZeroMQQueue = new ConcurrentQueue<List<string>>();
    protected readonly ConcurrentQueue<List<string>> ZeroMQQueueAudio = new ConcurrentQueue<List<string>>();

    protected abstract void ListenerWork();

    protected abstract void StartListening();

    protected abstract void StopListening();
}