using UnityEngine;

public class IpcController : MonoBehaviour
{
    public static IpcController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AvatarRandomMessages avatarMessages;

    private IpcMessageServer server;
    private const int MaxMessagesPerFrame = 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null)
            return;

#if UNITY_STANDALONE_LINUX
        var go = new GameObject("IpcController");
        DontDestroyOnLoad(go);
        go.AddComponent<IpcController>();
#endif
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (avatarMessages == null)
            avatarMessages = FindFirstObjectByType<AvatarRandomMessages>();

        server = new IpcMessageServer();
        server.Start();
    }

    void Update()
    {
        if (server == null)
            return;

        int processed = 0;
        while (processed < MaxMessagesPerFrame && server.CommandQueue.TryDequeue(out var command))
        {
            ProcessCommand(command);
            processed++;
        }
    }

    private void ProcessCommand(IpcQueuedCommand command)
    {
        IpcResponse response;

        try
        {
            if (command.Request.type == "show_message")
            {
                var target = GetActiveAvatarMessages();
                if (target == null)
                {
                    response = IpcProtocol.CreateError(command.Request.requestId, "AvatarRandomMessages not found");
                    command.Callback?.Invoke(response);
                    return;
                }

                var msg = new AvatarMessage
                {
                    text = command.Request.payload.text,
                    state = command.Request.payload.state ?? "Idle",
                    locKey = "",
                    onActive = false,
                    isHusbando = false
                };

                target.ShowExternalMessage(msg, command.Request.payload.forceShow);
                response = IpcProtocol.CreateSuccess(command.Request.requestId);
            }
            else
            {
                response = IpcProtocol.CreateError(command.Request.requestId, "Unknown command type");
            }
        }
        catch (System.Exception e)
        {
            response = IpcProtocol.CreateError(command.Request.requestId, "Internal error: " + e.Message);
            Debug.LogError("[IPC] Command processing error: " + e);
        }

        command.Callback?.Invoke(response);
    }

    private AvatarRandomMessages GetActiveAvatarMessages()
    {
        if (avatarMessages != null && avatarMessages.isActiveAndEnabled)
            return avatarMessages;

        var candidates = FindObjectsByType<AvatarRandomMessages>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && candidates[i].isActiveAndEnabled)
            {
                avatarMessages = candidates[i];
                return avatarMessages;
            }
        }

        if (candidates.Length > 0)
            avatarMessages = candidates[0];

        return avatarMessages;
    }

    void OnDestroy()
    {
        server?.Dispose();
        server = null;
    }

    void OnApplicationQuit()
    {
        server?.Dispose();
        server = null;
    }
}
