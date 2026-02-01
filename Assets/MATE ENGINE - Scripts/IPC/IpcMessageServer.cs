using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class IpcMessageServer : IDisposable
{
    private readonly string socketPath;
    private Socket listenerSocket;
    private Thread acceptThread;
    private volatile bool running;
    private readonly ConcurrentQueue<IpcQueuedCommand> commandQueue;

    private readonly object rateLimitLock = new object();
    private int messageCount;
    private float rateLimitWindowStart;
    private const int MaxMessagesPerSecond = 10;

    public ConcurrentQueue<IpcQueuedCommand> CommandQueue => commandQueue;

    public IpcMessageServer()
    {
        string runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (string.IsNullOrEmpty(runtimeDir))
            runtimeDir = "/tmp";

        socketPath = Path.Combine(runtimeDir, "mate-engine.sock");
        commandQueue = new ConcurrentQueue<IpcQueuedCommand>();
    }

    public void Start()
    {
        if (running)
            return;

        CleanupStaleSocket();

        try
        {
            listenerSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            listenerSocket.Bind(endpoint);

            SetSocketPermissions();

            listenerSocket.Listen(5);
            running = true;

            acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "IpcMessageServer"
            };
            acceptThread.Start();

            Debug.Log("[IPC] Server started at " + socketPath);
        }
        catch (Exception e)
        {
            Debug.LogError("[IPC] Failed to start server: " + e.Message);
            Cleanup();
        }
    }

    public void Stop()
    {
        running = false;
        Cleanup();
    }

    private void CleanupStaleSocket()
    {
        if (File.Exists(socketPath))
        {
            try
            {
                File.Delete(socketPath);
                Debug.Log("[IPC] Removed stale socket file");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IPC] Failed to remove stale socket: " + e.Message);
            }
        }
    }

    private void SetSocketPermissions()
    {
        try
        {
            var chmod = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = "600 \"" + socketPath + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(chmod)?.WaitForExit(1000);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[IPC] Failed to set socket permissions: " + e.Message);
        }
    }

    private void AcceptLoop()
    {
        while (running)
        {
            try
            {
                if (!running || listenerSocket == null)
                    break;

                if (!listenerSocket.Poll(100000, SelectMode.SelectRead))
                    continue;

                Socket clientSocket = listenerSocket.Accept();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(clientSocket));
            }
            catch (SocketException) when (!running)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                if (running)
                    Debug.LogWarning("[IPC] Accept error: " + e.Message);
            }
        }
    }

    private void HandleClient(Socket clientSocket)
    {
        try
        {
            clientSocket.ReceiveTimeout = 5000;
            clientSocket.SendTimeout = 5000;

            using var stream = new NetworkStream(clientSocket, true);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            string line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
                return;

            IpcRequest request = IpcProtocol.ParseRequest(line);
            string validationError = IpcProtocol.ValidateRequest(request);

            if (validationError != null)
            {
                var errorResponse = IpcProtocol.CreateError(request?.requestId, validationError);
                writer.WriteLine(IpcProtocol.SerializeResponse(errorResponse));
                return;
            }

            if (!CheckRateLimit())
            {
                var rateLimitResponse = IpcProtocol.CreateError(request.requestId, "Rate limit exceeded");
                writer.WriteLine(IpcProtocol.SerializeResponse(rateLimitResponse));
                return;
            }

            var responseEvent = new ManualResetEventSlim(false);
            IpcResponse response = null;

            var queuedCommand = new IpcQueuedCommand
            {
                Request = request,
                Callback = r =>
                {
                    response = r;
                    responseEvent.Set();
                }
            };

            commandQueue.Enqueue(queuedCommand);

            if (responseEvent.Wait(5000))
            {
                writer.WriteLine(IpcProtocol.SerializeResponse(response));
            }
            else
            {
                var timeoutResponse = IpcProtocol.CreateError(request.requestId, "Request timed out");
                writer.WriteLine(IpcProtocol.SerializeResponse(timeoutResponse));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[IPC] Client handling error: " + e.Message);
        }
    }

    private bool CheckRateLimit()
    {
        lock (rateLimitLock)
        {
            float now = Time.realtimeSinceStartup;

            if (now - rateLimitWindowStart >= 1f)
            {
                rateLimitWindowStart = now;
                messageCount = 0;
            }

            if (messageCount >= MaxMessagesPerSecond)
                return false;

            messageCount++;
            return true;
        }
    }

    private void Cleanup()
    {
        try
        {
            listenerSocket?.Close();
            listenerSocket?.Dispose();
            listenerSocket = null;
        }
        catch { }

        try
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
