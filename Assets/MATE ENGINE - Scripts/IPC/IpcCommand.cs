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
    // show_message fields
    public string text;
    public string state = "Idle";
    public bool forceShow;

    // optional animator control (for show_message or standalone set_animator)
    public IpcAnimatorParam[] animatorParams;
}

[Serializable]
public class IpcAnimatorParam
{
    public string param;
    public string valueType;
    public bool boolValue;
    public float floatValue;
    public int intValue;
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
