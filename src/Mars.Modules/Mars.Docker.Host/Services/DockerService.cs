using System.Globalization;
using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts;
using Mars.Docker.Host.Mappings;
using Mars.Docker.Host.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Mars.Docker.Host.Services;

public interface IDockerService
{
    // Container operations
    Task<ContainerListResponse?> GetContainer(string id, CancellationToken cancellationToken);
    Task<ContainerListResponse?> GetContainerByName(string name, CancellationToken cancellationToken);
    Task<ContainerInspectResponse?> InspectContainer(string id, CancellationToken cancellationToken);
    Task<ListDataResult<ContainerListResponse1>> ListContainers(ListContainerQuery query, CancellationToken cancellationToken);
    Task<PagingResult<ContainerListResponse1>> ListContainersTable(ListContainerQuery query, CancellationToken cancellationToken);
    Task<CreateContainerResponse> CreateContainer(CreateContainerQuery query, CancellationToken cancellationToken);
    Task<bool> StartContainer(string id, CancellationToken cancellationToken);
    Task<bool> StopContainer(string id, CancellationToken cancellationToken);
    Task RestartContainer(string id, CancellationToken cancellationToken);
    Task PauseContainer(string id, CancellationToken cancellationToken);
    Task UnpauseContainer(string id, CancellationToken cancellationToken);
    Task DeleteContainer(string id, CancellationToken cancellationToken);
    Task<DockerRunResultResponse> RunContainerOnce(RunContainerQuery query, CancellationToken cancellationToken);
    Task<DockerRunResultResponse> ExecInContainer(string containerId, ExecContainerQuery query, CancellationToken cancellationToken);
    Task<DockerRunResultResponse> WaitContainer(string id, int timeoutSeconds, CancellationToken cancellationToken);
    Task<DockerRunResultResponse> StartAndCapture(string id, int timeoutSeconds, bool removeAfterExit, CancellationToken cancellationToken);
    Task<DockerRunResultResponse?> GetRunResult(string id);
    Task<ContainerLogsResponse1> GetContainerLogs(string id, int tail, CancellationToken cancellationToken);

    // Image operations
    Task<ImageInspectResponse?> InspectImage(string name, CancellationToken cancellationToken);
    Task<ListDataResult<ImagesListResponse>> ListImages(ListImageQuery query, CancellationToken cancellationToken);
    Task<PagingResult<ImagesListResponse>> ListImagesTable(ListImageQuery query, CancellationToken cancellationToken);
    Task PullImage(string name, string tag, IProgress<string>? progress, CancellationToken cancellationToken);
    Task EnsureImage(string image, IProgress<string>? progress, CancellationToken cancellationToken);
    Task DeleteImage(string name, CancellationToken cancellationToken);

    // Volume operations
    Task<VolumeResponse?> InspectVolume(string name, CancellationToken cancellationToken);
    Task<ListDataResult<VolumeResponse>> ListVolumes(ListVolumeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<VolumeResponse>> ListVolumesTable(ListVolumeQuery query, CancellationToken cancellationToken);
    Task<VolumeResponse> CreateVolume(CreateVolumeQuery query, CancellationToken cancellationToken);
    Task DeleteVolume(string name, bool force, CancellationToken cancellationToken);
}

public class DockerService : IDockerService
{
    private static readonly TimeSpan RunResultCacheTtl = TimeSpan.FromMinutes(10);

    private readonly DockerClient _dockerClient;
    private readonly DockerOptions _options;
    private readonly IMemoryCache _memoryCache;

    public DockerService(DockerClient dockerClient, IOptions<DockerOptions> options, IMemoryCache memoryCache)
    {
        _dockerClient = dockerClient;
        _options = options.Value;
        _memoryCache = memoryCache;
    }

    // Container operations implementation
    public async Task<ContainerListResponse?> GetContainer(string id, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.FirstOrDefault(c => c.ID == id);
    }

    public async Task<ContainerListResponse?> GetContainerByName(string name, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.FirstOrDefault(c => c.Names.Any(n =>
            n.TrimStart('/').Equals(name.TrimStart('/'), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<ContainerInspectResponse?> InspectContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.InspectContainerAsync(id, cancellationToken);
    }

    public async Task<ListDataResult<ContainerListResponse1>> ListContainers(ListContainerQuery query, CancellationToken cancellationToken)
    {
        var containers = await FilterContainersAsync(query, cancellationToken);
        var result = containers.AsListDataResult(query).ToResponse();
        await EnrichStartedAtAsync(result.Items, cancellationToken);
        return result;
    }

    public async Task<PagingResult<ContainerListResponse1>> ListContainersTable(ListContainerQuery query, CancellationToken cancellationToken)
    {
        var containers = await FilterContainersAsync(query, cancellationToken);
        var result = containers.AsPagingResult(query).ToResponse();
        await EnrichStartedAtAsync(result.Items, cancellationToken);
        return result;
    }

    private async Task<IEnumerable<ContainerListResponse>> FilterContainersAsync(ListContainerQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.Where(s => query.Search == null || s.Names.Any(x => x.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
    }

    // docker list API не отдаёт время последнего старта — добираем из inspect (только для текущей страницы)
    private async Task EnrichStartedAtAsync(IEnumerable<ContainerListResponse1> items, CancellationToken cancellationToken)
    {
        await Task.WhenAll(items.Select(async item =>
        {
            try
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(item.ID, cancellationToken);
                if (DateTime.TryParse(inspect.State?.StartedAt, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var started)
                    && started > DateTime.MinValue)
                {
                    item.StartedAt = started;
                }
            }
            catch (DockerApiException)
            {
                // контейнер исчез между list и inspect
            }
        }));
    }

    public async Task<CreateContainerResponse> CreateContainer(CreateContainerQuery query, CancellationToken cancellationToken)
    {
        await EnsureImage(query.Image, null, cancellationToken);

        var image = DockerImageName.ResolveFull(query.Image, _options.RegistryUrl);
        var parameters = new CreateContainerParameters
        {
            Image = image,
            Name = query.Name,
            Cmd = query.Cmd.Count > 0 ? query.Cmd.ToList() : null,
            Env = query.Env.Count > 0 ? query.Env.ToList() : null,
            WorkingDir = query.WorkingDir,
            HostConfig = new HostConfig
            {
                AutoRemove = query.AutoRemove,
                RestartPolicy = ParseRestartPolicy(query.RestartPolicy),
            },
        };

        if (query.Ports.Count > 0)
        {
            parameters.ExposedPorts = query.Ports
                .ToDictionary(p => PortKey(p.ContainerPort, p.Protocol), _ => default(EmptyStruct));
            parameters.HostConfig.PortBindings = query.Ports
                .GroupBy(p => PortKey(p.ContainerPort, p.Protocol))
                .ToDictionary(
                    g => g.Key,
                    g => (IList<PortBinding>)g.Select(p => new PortBinding { HostPort = p.HostPort.ToString() }).ToList());
        }

        return await _dockerClient.Containers.CreateContainerAsync(parameters, cancellationToken);
    }

    public Task<bool> StartContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters(), cancellationToken);
    }

    public Task<bool> StopContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters(), cancellationToken);
    }

    public Task RestartContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.RestartContainerAsync(id, new ContainerRestartParameters(), cancellationToken);
    }

    public Task PauseContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.PauseContainerAsync(id, cancellationToken);
    }
    public Task UnpauseContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.UnpauseContainerAsync(id, cancellationToken);
    }

    public Task DeleteContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, cancellationToken);
    }

    public async Task<DockerRunResultResponse> RunContainerOnce(RunContainerQuery query, CancellationToken cancellationToken)
    {
        await EnsureImage(query.Image, null, cancellationToken);

        var hasStdin = query.Stdin is not null;
        var created = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = DockerImageName.ResolveFull(query.Image, _options.RegistryUrl),
            Cmd = query.Cmd.Count > 0 ? query.Cmd.ToList() : null,
            Env = query.Env.Count > 0 ? query.Env.ToList() : null,
            WorkingDir = query.WorkingDir,
            User = query.User,
            Tty = false,
            AttachStdin = hasStdin,
            AttachStdout = true,
            AttachStderr = true,
            OpenStdin = hasStdin,
            StdinOnce = hasStdin,
        }, cancellationToken);

        try
        {
            var timeout = TimeSpan.FromSeconds(query.TimeoutSeconds > 0 ? query.TimeoutSeconds : _options.DefaultRunTimeoutSeconds);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            long exitCode;
            var timedOut = false;
            try
            {
                if (hasStdin)
                {
                    // stdin attach + полное закрытие соединения: при StdinOnce демон сам доставит
                    // EOF контейнеру. CloseWrite поверх npipe ненадёжен — нуль-байтовое сообщение
                    // теряется, если данные ещё не прочитаны (go-winio#42, задевает и docker CLI).
                    var stream = await _dockerClient.Containers.AttachContainerAsync(
                        created.ID, false,
                        new ContainerAttachParameters { Stream = true, Stdin = true, Stdout = false, Stderr = false },
                        timeoutCts.Token);
                    await _dockerClient.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), timeoutCts.Token);
                    var bytes = Encoding.UTF8.GetBytes(query.Stdin!);
                    await stream.WriteAsync(bytes, 0, bytes.Length, timeoutCts.Token);
                    stream.Dispose();
                }
                else
                {
                    await _dockerClient.Containers.StartContainerAsync(created.ID, new ContainerStartParameters(), timeoutCts.Token);
                }

                var wait = await _dockerClient.Containers.WaitContainerAsync(created.ID, timeoutCts.Token);
                exitCode = wait.StatusCode;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                exitCode = await KillAndWaitAsync(created.ID);
            }

            var logs = await GetContainerLogs(created.ID, 0, CancellationToken.None);
            return new DockerRunResultResponse
            {
                ExitCode = exitCode,
                Stdout = logs.Stdout,
                Stderr = logs.Stderr,
                TimedOut = timedOut,
            };
        }
        finally
        {
            if (!query.KeepContainer)
            {
                await TryRemoveContainerAsync(created.ID);
            }
        }
    }

    private async Task<long> KillAndWaitAsync(string containerId)
    {
        try
        {
            await _dockerClient.Containers.KillContainerAsync(containerId, new ContainerKillParameters(), CancellationToken.None);
        }
        catch (DockerApiException)
        {
            // уже остановлен
        }

        try
        {
            var wait = await _dockerClient.Containers.WaitContainerAsync(containerId, CancellationToken.None);
            return wait.StatusCode;
        }
        catch (DockerApiException)
        {
            return -1;
        }
    }

    public async Task<DockerRunResultResponse> ExecInContainer(string containerId, ExecContainerQuery query, CancellationToken cancellationToken)
    {
        var hasStdin = query.Stdin is not null;
        var exec = await _dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
        {
            AttachStdin = hasStdin,
            AttachStdout = true,
            AttachStderr = true,
            Tty = false,
            Cmd = query.Cmd.ToList(),
            Env = query.Env.Count > 0 ? query.Env.ToList() : null,
            WorkingDir = query.WorkingDir,
            User = query.User,
        }, cancellationToken);

        return await RunAttachedAsync(
            async token =>
            {
                var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(exec.ID, false, token);
                return (stream, await SendStdinAndCollectAsync(stream, query.Stdin, token));
            },
            async token =>
            {
                var inspect = await _dockerClient.Exec.InspectContainerExecAsync(exec.ID, token);
                return inspect.ExitCode;
            },
            onTimeout: null,
            query.TimeoutSeconds,
            cancellationToken);
    }

    public async Task<DockerRunResultResponse> WaitContainer(string id, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : _options.DefaultRunTimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        long exitCode;
        var timedOut = false;
        try
        {
            var wait = await _dockerClient.Containers.WaitContainerAsync(id, timeoutCts.Token);
            exitCode = wait.StatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            exitCode = -1;
        }

        var logs = await GetContainerLogs(id, 0, CancellationToken.None);
        return new DockerRunResultResponse
        {
            ExitCode = exitCode,
            Stdout = logs.Stdout,
            Stderr = logs.Stderr,
            TimedOut = timedOut,
        };
    }

    /// <summary>
    /// Старт контейнера с сохранением результата выполнения в кеше: контейнер создаётся БЕЗ
    /// AutoRemove (иначе демон удаляет логи вместе с ним), поэтому после wait вывод читается
    /// штатно, кэшируется для страницы контейнера, и только затем контейнер удаляется.
    /// </summary>
    public async Task<DockerRunResultResponse> StartAndCapture(string id, int timeoutSeconds, bool removeAfterExit, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : _options.DefaultRunTimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        await _dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters(), timeoutCts.Token);

        long exitCode;
        var timedOut = false;
        try
        {
            var wait = await _dockerClient.Containers.WaitContainerAsync(id, timeoutCts.Token);
            exitCode = wait.StatusCode;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            exitCode = -1;
        }

        var logs = await GetContainerLogs(id, 0, CancellationToken.None);
        var result = new DockerRunResultResponse
        {
            ExitCode = exitCode,
            Stdout = logs.Stdout,
            Stderr = logs.Stderr,
            TimedOut = timedOut,
        };

        _memoryCache.Set(RunResultCacheKey(id), result, RunResultCacheTtl);

        if (removeAfterExit && !timedOut)
        {
            await TryRemoveContainerAsync(id);
        }

        return result;
    }

    public Task<DockerRunResultResponse?> GetRunResult(string id)
        => Task.FromResult(_memoryCache.TryGetValue<DockerRunResultResponse>(RunResultCacheKey(id), out var result) ? result : null);

    private static string RunResultCacheKey(string id) => $"docker:run-result:{id}";

    public async Task<ContainerLogsResponse1> GetContainerLogs(string id, int tail, CancellationToken cancellationToken)
    {
        var parameters = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Tail = tail > 0 ? tail.ToString() : "all",
        };
        using var stream = await _dockerClient.Containers.GetContainerLogsAsync(id, false, parameters, cancellationToken);
        var stdout = new MemoryStream();
        var stderr = new MemoryStream();
        await stream.CopyOutputToAsync(null, stdout, stderr, cancellationToken);
        return new ContainerLogsResponse1
        {
            Stdout = Encoding.UTF8.GetString(stdout.ToArray()),
            Stderr = Encoding.UTF8.GetString(stderr.ToArray()),
        };
    }

    /// <summary>
    /// Общий каркас attach-выполнения: старт потока, сбор stdout/stderr, ожидание exit code,
    /// обработка таймаута.
    /// </summary>
    private async Task<DockerRunResultResponse> RunAttachedAsync(
        Func<CancellationToken, Task<(MultiplexedStream Stream, (MemoryStream Stdout, MemoryStream Stderr) Output)>> startAndCollect,
        Func<CancellationToken, Task<long>> getExitCode,
        Func<CancellationToken, Task>? onTimeout,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : _options.DefaultRunTimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var timedOut = false;
        (MemoryStream Stdout, MemoryStream Stderr)? output = null;
        try
        {
            var (stream, collected) = await startAndCollect(timeoutCts.Token);
            using (stream)
            {
                await stream.CopyOutputToAsync(null, collected.Stdout, collected.Stderr, timeoutCts.Token);
            }
            output = collected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            if (onTimeout is not null)
            {
                await onTimeout(cancellationToken);
            }
        }

        long exitCode;
        try
        {
            exitCode = await getExitCode(cancellationToken);
        }
        catch (Exception) when (timedOut)
        {
            exitCode = -1;
        }

        return new DockerRunResultResponse
        {
            ExitCode = exitCode,
            Stdout = output is null ? string.Empty : Encoding.UTF8.GetString(output.Value.Stdout.ToArray()),
            Stderr = output is null ? string.Empty : Encoding.UTF8.GetString(output.Value.Stderr.ToArray()),
            TimedOut = timedOut,
        };
    }

    private static async Task<(MemoryStream Stdout, MemoryStream Stderr)> SendStdinAndCollectAsync(
        MultiplexedStream stream, string? stdin, CancellationToken cancellationToken)
    {
        var stdout = new MemoryStream();
        var stderr = new MemoryStream();
        if (stdin is not null)
        {
            // exec не имеет logs-API, поэтому только CloseWrite; на Windows npipe EOF может
            // не дойти (go-winio#42) — команды, ждущие EOF stdin, для exec ненадёжны,
            // для них использовать RunContainerOnce.
            var bytes = Encoding.UTF8.GetBytes(stdin);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            stream.CloseWrite();
        }

        return (stdout, stderr);
    }

    private async Task TryRemoveContainerAsync(string id)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(
                id, new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, CancellationToken.None);
        }
        catch (DockerContainerNotFoundException)
        {
            // уже удалён
        }
    }

    // Image operations implementation
    public Task<ImageInspectResponse?> InspectImage(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Images.InspectImageAsync(DockerImageName.ResolveFull(name, _options.RegistryUrl), cancellationToken);
    }

    public async Task<ListDataResult<ImagesListResponse>> ListImages(ListImageQuery query, CancellationToken cancellationToken)
    {
        var images = await ListImagesCoreAsync(query, cancellationToken);
        return images.AsListDataResult(query);
    }

    public async Task<PagingResult<ImagesListResponse>> ListImagesTable(ListImageQuery query, CancellationToken cancellationToken)
    {
        var images = await ListImagesCoreAsync(query, cancellationToken);
        return images.AsPagingResult(query);
    }

    private async Task<IEnumerable<ImagesListResponse>> ListImagesCoreAsync(ListImageQuery query, CancellationToken cancellationToken)
    {
        var images = await _dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = false, Digests = true }, cancellationToken);
        return images.Where(s => query.Search == null
                                 || s.RepoTags?.Any(t => t.Contains(query.Search, StringComparison.OrdinalIgnoreCase)) == true);
    }

    public async Task PullImage(string name, string tag, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var fromImage = DockerImageName.ApplyRegistry(name, _options.RegistryUrl);
        var authConfig = string.IsNullOrEmpty(_options.RegistryUser)
            ? null
            : new AuthConfig
            {
                Username = _options.RegistryUser,
                Password = _options.RegistryPassword,
                ServerAddress = string.IsNullOrEmpty(_options.RegistryUrl) ? "https://index.docker.io/v1/" : _options.RegistryUrl,
            };

        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = fromImage, Tag = string.IsNullOrEmpty(tag) ? "latest" : tag },
            authConfig,
            progress is null ? new Progress<JSONMessage>() : new Progress<JSONMessage>(m => progress.Report(PullProgressText(m))),
            cancellationToken);
    }

    public async Task EnsureImage(string image, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var full = DockerImageName.ResolveFull(image, _options.RegistryUrl);
        try
        {
            await _dockerClient.Images.InspectImageAsync(full, cancellationToken);
            return;
        }
        catch (DockerImageNotFoundException)
        {
            // тянем ниже
        }

        var (name, tag) = DockerImageName.SplitTag(image);
        await PullImage(name, tag, progress, cancellationToken);
    }

    public Task DeleteImage(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Images.DeleteImageAsync(
            DockerImageName.ResolveFull(name, _options.RegistryUrl), new ImageDeleteParameters { Force = true }, cancellationToken);
    }

    private static string PullProgressText(JSONMessage message)
        => !string.IsNullOrEmpty(message.ProgressMessage) ? message.ProgressMessage : message.Status;

    private static RestartPolicy? ParseRestartPolicy(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var kind = name.ToLowerInvariant().Replace("-", "") switch
        {
            "no" => RestartPolicyKind.No,
            "always" => RestartPolicyKind.Always,
            "onfailure" => RestartPolicyKind.OnFailure,
            "unlessstopped" => RestartPolicyKind.UnlessStopped,
            _ => throw new ArgumentException($"Unknown restart policy: {name}", nameof(name)),
        };
        return new RestartPolicy { Name = kind };
    }

    private static string PortKey(int port, string protocol)
        => $"{port}/{(string.IsNullOrEmpty(protocol) ? "tcp" : protocol.ToLowerInvariant())}";

    // Volume operations implementation
    public Task<VolumeResponse?> InspectVolume(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Volumes.InspectAsync(name, cancellationToken);
    }

    public async Task<ListDataResult<VolumeResponse>> ListVolumes(ListVolumeQuery query, CancellationToken cancellationToken)
    {
        var volumes = await _dockerClient.Volumes.ListAsync(
            new VolumesListParameters { }, cancellationToken);
        return volumes.Volumes.AsListDataResult(query);
    }

    public async Task<PagingResult<VolumeResponse>> ListVolumesTable(ListVolumeQuery query, CancellationToken cancellationToken)
    {
        var volumes = await _dockerClient.Volumes.ListAsync(
            new VolumesListParameters { }, cancellationToken);
        return volumes.Volumes.AsPagingResult(query);
    }

    public async Task<VolumeResponse> CreateVolume(CreateVolumeQuery query, CancellationToken cancellationToken)
    {
        return await _dockerClient.Volumes.CreateAsync(
            new VolumesCreateParameters
            {
                Name = query.Name,
                Labels = query.Labels.ToDictionary(),
                Driver = query.Driver,
                DriverOpts = query.DriverOpts.ToDictionary(),
            },
            cancellationToken);
    }

    public Task DeleteVolume(string name, bool force, CancellationToken cancellationToken)
    {
        return _dockerClient.Volumes.RemoveAsync(name, force, cancellationToken);
    }
}
