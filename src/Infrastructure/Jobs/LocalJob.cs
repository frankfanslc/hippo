using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Hippo.Application.Common.Exceptions;
using Hippo.Application.Jobs;
using Microsoft.Extensions.Configuration;

namespace Hippo.Infrastructure.Jobs;

public class LocalJob : Job
{
    private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
    private readonly string bindleId;
    private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
    private readonly string bindleUrl;
    private readonly string wagiBinaryPath;
    private Process? process;

    public LocalJob(IConfiguration configuration, Guid id, string bindleId) : base(id)
    {
        this.bindleId = bindleId;
        bindleUrl = configuration.GetValue<string>("Bindle:Url", "http://127.0.0.1:8080/v1");
        wagiBinaryPath = configuration.GetValue<string>("Wagi:BinaryPath", (OperatingSystem.IsWindows() ? "wagi.exe" : "wagi"));
    }

    public void AddEnvironmentVariable(string key, string value)
    {
        environmentVariables[key] = value;
    }

    public override void Run()
    {
        try
        {
            process = Process.Start(psi());
            if (process == null)
            {
                throw new JobFailedException("Job never started");
            }

            process.Start();
            _status = JobStatus.Running;

            // kill the process if the calling thread cancels the Job.
            cancellationToken.Register(() =>
            {
                process.Kill();
                _status = JobStatus.Canceled;
            });
        }
        catch (Win32Exception e)  // yes, even on Linux
        {
            if (e.Message.Contains("No such file or directory", StringComparison.InvariantCultureIgnoreCase) || e.Message.Contains("The system cannot find the file specified", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"The system cannot find '{wagiBinaryPath}'; add '{wagiBinaryPath}' to your $PATH or set 'Nomad:BinaryPath' in your appsettings to the correct location.");
            }
            throw;
        }
    }

    private ProcessStartInfo psi()
    {
        var env = String.Join(' ', environmentVariables.Select(ev => $"--env {ev.Key}=\"{ev.Value}\""));

        return new ProcessStartInfo
        {
            FileName = wagiBinaryPath,
            Arguments = $"-b {bindleId} --bindle-url {bindleUrl} -l 127.0.0.1:{GetAvailablePort()} {env}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
    }

    public override void Stop()
    {
        // no need to tell wagi to stop if the process hasn't started
        if (IsWaitingToRun || IsCompleted)
        {
            return;
        }

        process?.Kill();
        _status = JobStatus.Completed;
    }

    private static int GetAvailablePort()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }
    }
}
