namespace MarsDocs.DevServer.Services;

public class SseManager
{
    private readonly List<StreamWriter> _clients = [];
    private readonly object _lock = new();

    public void AddClient(StreamWriter client)
    {
        lock (_lock)
        {
            _clients.Add(client);
        }
    }

    public void RemoveClient(StreamWriter client)
    {
        lock (_lock)
        {
            _clients.Remove(client);
        }
    }

    public async Task BroadcastAsync(string message)
    {
        List<StreamWriter> clientsCopy;

        lock (_lock)
        {
            clientsCopy = _clients.ToList();
        }

        foreach (var client in clientsCopy)
        {
            try
            {
                await client.WriteAsync($"data: {message}\n\n");
                await client.FlushAsync();
            }
            catch
            {
                RemoveClient(client);
            }
        }
    }
}
