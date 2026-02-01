using UnityEngine;

public class IpcController : MonoBehaviour
{
    public static IpcController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AvatarRandomMessages avatarMessages;
    [SerializeField] private Animator avatarAnimator;

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
            switch (command.Request.type)
            {
                case "show_message":
                    response = HandleShowMessage(command.Request);
                    break;
                case "set_animator":
                    response = HandleSetAnimator(command.Request);
                    break;
                default:
                    response = IpcProtocol.CreateError(command.Request.requestId, "Unknown command type");
                    break;
            }
        }
        catch (System.Exception e)
        {
            response = IpcProtocol.CreateError(command.Request.requestId, "Internal error: " + e.Message);
            Debug.LogError("[IPC] Command processing error: " + e);
        }

        command.Callback?.Invoke(response);
    }

    private IpcResponse HandleShowMessage(IpcRequest request)
    {
        var target = GetActiveAvatarMessages();
        if (target == null)
            return IpcProtocol.CreateError(request.requestId, "AvatarRandomMessages not found");

        // Apply optional animator params first
        if (request.payload.animatorParams != null && request.payload.animatorParams.Length > 0)
        {
            string err = ApplyAnimatorParams(request.payload.animatorParams, request.requestId);
            if (err != null)
                return IpcProtocol.CreateError(request.requestId, err);
        }

        var msg = new AvatarMessage
        {
            text = request.payload.text,
            state = request.payload.state ?? "Idle",
            locKey = "",
            onActive = false,
            isHusbando = false
        };

        target.ShowExternalMessage(msg, request.payload.forceShow);
        return IpcProtocol.CreateSuccess(request.requestId);
    }

    private IpcResponse HandleSetAnimator(IpcRequest request)
    {
        string err = ApplyAnimatorParams(request.payload.animatorParams, request.requestId);
        if (err != null)
            return IpcProtocol.CreateError(request.requestId, err);

        return IpcProtocol.CreateSuccess(request.requestId);
    }

    private string ApplyAnimatorParams(IpcAnimatorParam[] animatorParams, string requestId)
    {
        var animator = GetActiveAnimator();
        if (animator == null)
            return "Animator not found";

        for (int i = 0; i < animatorParams.Length; i++)
        {
            var p = animatorParams[i];

            if (!HasAnimatorParameter(animator, p.param))
                return "Unknown animator parameter: " + p.param;

            switch (p.valueType)
            {
                case "bool":
                    animator.SetBool(p.param, p.boolValue);
                    break;
                case "float":
                    animator.SetFloat(p.param, p.floatValue);
                    break;
                case "int":
                    animator.SetInteger(p.param, p.intValue);
                    break;
                case "trigger":
                    animator.SetTrigger(p.param);
                    break;
            }
        }

        return null;
    }

    private bool HasAnimatorParameter(Animator animator, string paramName)
    {
        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName)
                return true;
        }
        return false;
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

    private Animator GetActiveAnimator()
    {
        if (avatarAnimator != null && avatarAnimator.isActiveAndEnabled)
            return avatarAnimator;

        var candidates = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && candidates[i].isActiveAndEnabled &&
                candidates[i].runtimeAnimatorController != null)
            {
                avatarAnimator = candidates[i];
                return avatarAnimator;
            }
        }

        return null;
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
