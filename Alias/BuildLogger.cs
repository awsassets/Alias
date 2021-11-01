using Microsoft.Build.Framework;

public class BuildLogger :
    ILogger
{
    IBuildEngine buildEngine;

    public BuildLogger(IBuildEngine buildEngine)
    {
        this.buildEngine = buildEngine;
    }

    public virtual void LogDebug(string message)
    {
        buildEngine.LogMessageEvent(new(message, "", "Alias", MessageImportance.Low));
    }

    public virtual void LogInfo(string message)
    {
        buildEngine.LogMessageEvent(new(message, "", "Alias", MessageImportance.Normal));
    }

    public virtual void LogWarning(string message)
    {
        buildEngine.LogWarningEvent(new("", "", null, 0, 0, 0, 0, message, "", "Alias"));
    }

    public virtual void LogError(string message)
    {
        buildEngine.LogErrorEvent(new("", "", null, 0, 0, 0, 0, message, "", "Alias"));
    }
}
