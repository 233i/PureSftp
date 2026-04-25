using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PureSFTP.Models;

namespace PureSFTP.Services;

public sealed class SqliteConnectionRepository : IConnectionRepository
{
    private readonly string _databasePath;

    public SqliteConnectionRepository(string databasePath)
    {
        _databasePath = databasePath;
    }

    public IReadOnlyList<SavedConnection> GetAll()
    {
        var connections = new List<SavedConnection>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, host, port, username, password, created_at, updated_at
            FROM saved_connections
            ORDER BY updated_at DESC, id DESC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            connections.Add(new SavedConnection
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Host = reader.GetString(2),
                Port = reader.GetInt32(3),
                Username = reader.GetString(4),
                Password = reader.GetString(5),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(6)),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(7)),
            });
        }

        return connections;
    }

    public SavedConnection Add(SavedConnection savedConnection)
    {
        var now = DateTimeOffset.Now;

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO saved_connections (name, host, port, username, password, created_at, updated_at)
            VALUES ($name, $host, $port, $username, $password, $createdAt, $updatedAt);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", savedConnection.Name);
        command.Parameters.AddWithValue("$host", savedConnection.Host);
        command.Parameters.AddWithValue("$port", savedConnection.Port);
        command.Parameters.AddWithValue("$username", savedConnection.Username);
        command.Parameters.AddWithValue("$password", savedConnection.Password);
        command.Parameters.AddWithValue("$createdAt", now.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", now.ToString("O"));

        var id = (long)(command.ExecuteScalar() ?? 0L);
        return new SavedConnection
        {
            Id = id,
            Name = savedConnection.Name,
            Host = savedConnection.Host,
            Port = savedConnection.Port,
            Username = savedConnection.Username,
            Password = savedConnection.Password,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_databasePath}");
    }
}
