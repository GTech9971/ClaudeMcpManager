namespace ClaudeMcpManager.Models;

/// <summary>
/// コマンド実行結果
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int ExitCode { get; set; }
    public Exception? Exception { get; set; }

    public static CommandResult CreateSuccess(string message = "")
    {
        return new CommandResult
        {
            Success = true,
            Message = message,
            ExitCode = 0
        };
    }

    public static CommandResult CreateError(string message, int exitCode = 1, Exception? exception = null)
    {
        return new CommandResult
        {
            Success = false,
            Message = message,
            ExitCode = exitCode,
            Exception = exception
        };
    }
}

