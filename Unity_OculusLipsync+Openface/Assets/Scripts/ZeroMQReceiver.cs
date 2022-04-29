using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public abstract class ZeroMQReceiver : MonoBehaviour
{
    public static Action<List<string>> NewZeroMQMessageReceivedEvent;
    public static Action<List<string>> NewZeroMQMessageReceivedEventAudio;
    public string OriginIP = "127.0.0.1";
    protected string OriginPort;
    protected bool ListenerThreadCancelled;
    public bool StartListeningOnAwake = true;

    protected readonly ConcurrentQueue<List<string>> ZeroMQQueue = new ConcurrentQueue<List<string>>();

    abstract protected void ListenerWork();

    abstract protected void StartListening();

    abstract protected void StopListening();
}