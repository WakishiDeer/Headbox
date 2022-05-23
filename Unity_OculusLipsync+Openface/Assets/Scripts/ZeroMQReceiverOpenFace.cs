using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using UnityEngine;

public class ZeroMQReceiverOpenFace : ZeroMQReceiver
{
    private string OriginPort = "5571";

    private Thread ZeroMQListenerThread;

    void Awake()
    {
        if (startListeningOnAwake)
        {
            StartListening();
        }
    }


    void Update()
    {
        while (!ZeroMQQueueOpenFace.IsEmpty)
        {
            List<string> msg_list;
            if (ZeroMQQueueOpenFace.TryDequeue(out msg_list))
            {
                NewZeroMQMessageReceivedEventOpenFace?.Invoke(msg_list);
            }
            else
            {
                break;
            }
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }


    protected override void ListenerWork()
    {
        AsyncIO.ForceDotNet.Force();
        Debug.Log("Waiting for subscribers");
        using (var subSocket = new SubscriberSocket())
        {
            // set limit on how many messages in memory
            subSocket.Options.ReceiveHighWatermark = 1000;
            // socket connection
            string connectionStr = "tcp://" + OriginIP + ":" + OriginPort;
            subSocket.Connect(connectionStr);
            // subscribe to topics; "" == all topics
            subSocket.Subscribe("");
            Debug.Log($"Connected to {connectionStr}");

            while (!ListenerThreadCancelled)
            {
                List<string> msg_list = new List<string>();
                // Try to receive two packets; one for the descriptor and one for the frame. 
                if (!subSocket.TryReceiveFrameString(out string frameData)) continue;

                if (!string.IsNullOrEmpty(frameData))
                {
                    // Debug.Log($"Added Frame to {frameData}");
                    msg_list.Add(frameData);
                }

                if (msg_list.Count > 0)
                {
                    ZeroMQQueueOpenFace.Enqueue(msg_list);
                }
            }

            Debug.Log("Closing subscriber socket");
            subSocket.Close();
        }

        Debug.Log("Cleaning up");
        NetMQConfig.Cleanup();
    }

    protected override void StartListening()
    {
        ListenerThreadCancelled = false;
        ZeroMQListenerThread = new Thread(ListenerWork);
        ZeroMQListenerThread.Start();
    }

    protected override void StopListening()
    {
        ListenerThreadCancelled = true;
        ZeroMQListenerThread.Join();
    }
}