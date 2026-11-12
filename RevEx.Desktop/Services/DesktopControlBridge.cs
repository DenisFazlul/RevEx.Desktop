using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Connector.Client;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.Desktop.Services;

public sealed class DesktopControlBridge : IAsyncDisposable
{
    public const string AccessTokenHeader = "X-RevEx-Desktop-Token";

    private static readonly JsonSerializerOptions RegistrationJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IConnectorClient _connectorClient;
    private readonly DesktopControlOptions _options;
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly DateTimeOffset _startedAtUtc = DateTimeOffset.UtcNow;
    private readonly string _accessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private WebApplication? _application;

    public DesktopControlBridge(
        IConnectorRegistry connectorRegistry,
        IConnectorClient connectorClient,
        DesktopControlOptions options)
    {
        _connectorRegistry = connectorRegistry;
        _connectorClient = connectorClient;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_application is not null)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });

        var application = builder.Build();
        ConfigureApplication(application);

        try
        {
            await application.StartAsync(cancellationToken);
            var address = GetServerAddress(application);
            await WriteRegistrationAsync(address, cancellationToken);
            _application = application;
        }
        catch
        {
            await application.DisposeAsync();
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var application = _application;
        _application = null;

        try
        {
            if (application is not null)
            {
                await application.StopAsync(cancellationToken);
                await application.DisposeAsync();
            }
        }
        finally
        {
            DeleteOwnRegistration();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private void ConfigureApplication(WebApplication application)
    {
        application.Use(async (context, next) =>
        {
            var suppliedToken = context.Request.Headers[AccessTokenHeader].ToString();
            if (!string.Equals(suppliedToken, _accessToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        });

        application.MapGet("/health", () => Results.Ok(new DesktopControlHealth(
            _instanceId,
            Environment.ProcessId,
            _startedAtUtc)));

        application.MapGet("/connectors", () =>
        {
            var connectors = _connectorRegistry
                .GetAll()
                .Select(connector => new DesktopConnectorDescription(
                    connector.InstanceId,
                    connector.Name,
                    connector.Product.ToString(),
                    connector.ProductVersion,
                    connector.ProcessId,
                    connector.ActiveDocument))
                .ToArray();

            return Results.Ok(connectors);
        });

        application.MapGet(
            "/connectors/{instanceId:guid}/commands",
            GetConnectorCommandsAsync);

        application.MapPost(
            "/connectors/{instanceId:guid}/commands/{command}",
            ExecuteConnectorCommandAsync);
    }

    private async Task<IResult> GetConnectorCommandsAsync(
        Guid instanceId,
        CancellationToken cancellationToken)
    {
        var connector = FindConnector(instanceId);
        if (connector is null)
        {
            return Results.NotFound(new { message = "Connector is not registered in Desktop." });
        }

        var task = new ConnectorTask(
            Guid.NewGuid(),
            "Get connector commands",
            ConnectorCommand.GetCommands);
        var commands = await _connectorClient.ExecuteAsync<object, ConnectorCommand[]>(
            connector,
            task,
            new object(),
            cancellationToken);
        var commandNames = commands
            .Select(command => new
            {
                name = command.ToString(),
                protocolName = GetProtocolName(command)
            })
            .ToArray();

        return Results.Ok(commandNames);
    }

    private async Task<IResult> ExecuteConnectorCommandAsync(
        Guid instanceId,
        string command,
        ExecuteDesktopConnectorCommandRequest request,
        CancellationToken cancellationToken)
    {
        var connector = FindConnector(instanceId);
        if (connector is null)
        {
            return Results.NotFound(new { message = "Connector is not registered in Desktop." });
        }

        if (!TryParseCommand(command, out var connectorCommand))
        {
            return Results.BadRequest(new { message = $"Unknown connector command '{command}'." });
        }

        var supportedCommands = await GetSupportedCommandsAsync(connector, cancellationToken);
        if (!supportedCommands.Contains(connectorCommand))
        {
            return Results.BadRequest(new
            {
                message = $"Connector does not support command '{connectorCommand}'."
            });
        }

        var taskId = Guid.NewGuid();
        var taskName = string.IsNullOrWhiteSpace(request.TaskName)
            ? connectorCommand.ToString()
            : request.TaskName!;
        var task = new ConnectorTask(taskId, taskName, connectorCommand);
        var payload = request.Payload.ValueKind == JsonValueKind.Undefined
            ? JValue.CreateNull()
            : JToken.Parse(request.Payload.GetRawText());
        var result = await _connectorClient.ExecuteAsync<JToken, JToken>(
            connector,
            task,
            payload,
            cancellationToken);
        var response = new JObject
        {
            ["taskId"] = taskId,
            ["command"] = connectorCommand.ToString(),
            ["result"] = result
        };

        return Results.Text(
            response.ToString(Formatting.None),
            "application/json");
    }

    private async Task<ConnectorCommand[]> GetSupportedCommandsAsync(
        ConnectorDescriptor connector,
        CancellationToken cancellationToken)
    {
        var task = new ConnectorTask(
            Guid.NewGuid(),
            "Get connector commands",
            ConnectorCommand.GetCommands);
        var commands = await _connectorClient.ExecuteAsync<object, ConnectorCommand[]>(
            connector,
            task,
            new object(),
            cancellationToken);

        return commands;
    }

    private ConnectorDescriptor? FindConnector(Guid instanceId)
    {
        return _connectorRegistry
            .GetAll()
            .FirstOrDefault(connector => connector.InstanceId == instanceId);
    }

    private static bool TryParseCommand(string value, out ConnectorCommand command)
    {
        if (Enum.TryParse(value, true, out command) && Enum.IsDefined(command))
        {
            return true;
        }

        foreach (var candidate in Enum.GetValues<ConnectorCommand>())
        {
            if (string.Equals(GetProtocolName(candidate), value, StringComparison.OrdinalIgnoreCase))
            {
                command = candidate;
                return true;
            }
        }

        command = default;
        return false;
    }

    private static string GetProtocolName(ConnectorCommand command)
    {
        var member = typeof(ConnectorCommand).GetMember(command.ToString()).Single();
        var attribute = member.GetCustomAttribute<EnumMemberAttribute>();

        return attribute?.Value ?? command.ToString();
    }

    private static string GetServerAddress(WebApplication application)
    {
        var server = application.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.SingleOrDefault();

        return address ?? throw new InvalidOperationException(
            "Desktop control server did not publish its loopback address.");
    }

    private async Task WriteRegistrationAsync(
        string address,
        CancellationToken cancellationToken)
    {
        var registration = new DesktopControlRegistration(
            _instanceId,
            Environment.ProcessId,
            address,
            _accessToken,
            _startedAtUtc);
        var json = System.Text.Json.JsonSerializer.Serialize(
            registration,
            RegistrationJsonOptions);
        var directory = Path.GetDirectoryName(_options.RegistrationFile)
            ?? throw new InvalidOperationException("Desktop registration directory is missing.");
        var temporaryFile = $"{_options.RegistrationFile}.{_instanceId:N}.tmp";

        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(temporaryFile, json, cancellationToken);
            File.Move(temporaryFile, _options.RegistrationFile, true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private void DeleteOwnRegistration()
    {
        try
        {
            if (!File.Exists(_options.RegistrationFile))
            {
                return;
            }

            var json = File.ReadAllText(_options.RegistrationFile);
            var registration = System.Text.Json.JsonSerializer.Deserialize<DesktopControlRegistration>(
                json,
                RegistrationJsonOptions);
            if (registration?.InstanceId == _instanceId)
            {
                File.Delete(_options.RegistrationFile);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (System.Text.Json.JsonException)
        {
        }
    }
}
