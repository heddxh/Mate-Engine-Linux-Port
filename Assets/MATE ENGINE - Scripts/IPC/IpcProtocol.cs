using UnityEngine;

public static class IpcProtocol
{
    public const int MaxTextLength = 2048;

    public static IpcRequest ParseRequest(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonUtility.FromJson<IpcRequest>(json);
        }
        catch
        {
            return null;
        }
    }

    public static string SerializeResponse(IpcResponse response)
    {
        return JsonUtility.ToJson(response);
    }

    public static IpcResponse CreateSuccess(string requestId)
    {
        return new IpcResponse
        {
            v = 1,
            requestId = requestId,
            ok = true,
            error = null
        };
    }

    public static IpcResponse CreateError(string requestId, string error)
    {
        return new IpcResponse
        {
            v = 1,
            requestId = requestId,
            ok = false,
            error = error
        };
    }

    public static string ValidateRequest(IpcRequest request)
    {
        if (request == null)
            return "Invalid JSON";

        if (request.v != 1)
            return "Unsupported protocol version";

        if (string.IsNullOrEmpty(request.type))
            return "Missing type field";

        if (request.type != "show_message")
            return "Unknown command type: " + request.type;

        if (request.payload == null)
            return "Missing payload";

        if (string.IsNullOrEmpty(request.payload.text))
            return "Missing text in payload";

        if (request.payload.text.Length > MaxTextLength)
            return "Text exceeds maximum length of " + MaxTextLength;

        return null;
    }
}
