using ClaudeMcpManager.Models;
using ClaudeMcpManager.Services;

namespace ClaudeMcpManager.Infrastructure;

/// <summary>
/// コンソール出力管理の実装
/// </summary>
public class ConsoleService : IConsoleService
{
    public void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteResult(CommandResult result)
    {
        if (result.Success)
        {
            WriteSuccess(result.Message);
        }
        else
        {
            WriteError(result.Message);
        }
    }

    public void ResetColor()
    {
        Console.ResetColor();
    }
}
