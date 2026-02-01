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

    private static readonly string[] ValidValueTypes = { "bool", "float", "int", "trigger" };

    public static string ValidateRequest(IpcRequest request)
    {
        if (request == null)
            return "Invalid JSON";

        if (request.v != 1)
            return "Unsupported protocol version";

        if (string.IsNullOrEmpty(request.type))
            return "Missing type field";

        if (request.payload == null)
            return "Missing payload";

        switch (request.type)
        {
            case "show_message":
                return ValidateShowMessage(request.payload);
            case "set_animator":
                return ValidateSetAnimator(request.payload);
            default:
                return "Unknown command type: " + request.type;
        }
    }

    private static string ValidateShowMessage(IpcPayload payload)
    {
        if (string.IsNullOrEmpty(payload.text))
            return "Missing text in payload";

        if (payload.text.Length > MaxTextLength)
            return "Text exceeds maximum length of " + MaxTextLength;

        // animatorParams is optional for show_message
        if (payload.animatorParams != null)
        {
            string err = ValidateAnimatorParams(payload.animatorParams);
            if (err != null)
                return err;
        }

        return null;
    }

    private static string ValidateSetAnimator(IpcPayload payload)
    {
        if (payload.animatorParams == null || payload.animatorParams.Length == 0)
            return "Missing animatorParams in payload";

        return ValidateAnimatorParams(payload.animatorParams);
    }

    private static string ValidateAnimatorParams(IpcAnimatorParam[] animatorParams)
    {
        for (int i = 0; i < animatorParams.Length; i++)
        {
            var p = animatorParams[i];
            if (string.IsNullOrEmpty(p.param))
                return "Missing param in animatorParams[" + i + "]";

            if (string.IsNullOrEmpty(p.valueType))
                return "Missing valueType in animatorParams[" + i + "]";

            bool valid = false;
            for (int j = 0; j < ValidValueTypes.Length; j++)
            {
                if (p.valueType == ValidValueTypes[j])
                {
                    valid = true;
                    break;
                }
            }

            if (!valid)
                return "Invalid valueType in animatorParams[" + i + "]: " + p.valueType;
        }

        return null;
    }
}
