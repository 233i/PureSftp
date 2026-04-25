using System;

namespace PureSFTP.Models;

public sealed class SavedConnection
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 22;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.Now;

    public SftpConnectionOptions ToOptions()
    {
        return new SftpConnectionOptions
        {
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
        };
    }
}
