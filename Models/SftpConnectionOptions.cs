namespace PureSFTP.Models;

public sealed class SftpConnectionOptions
{
    public required string Host { get; init; }

    public int Port { get; init; } = 22;

    public required string Username { get; init; }

    public required string Password { get; init; }

    public string StartupPath { get; init; } = string.Empty;
}
