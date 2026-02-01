using System;

[Serializable]
public class IpcRequest
{
    public int v = 1;
    public string type;
    public string requestId;
    public IpcPayload payload;
}

[Serializable]
public class IpcPayload
{
    public string text;
    public string state = "Idle";
    public bool forceShow;
}

[Serializable]
public class IpcResponse
{
    public int v = 1;
    public string requestId;
    public bool ok;
    public string error;
}

public class IpcQueuedCommand
{
    public IpcRequest Request;
    public Action<IpcResponse> Callback;
}
