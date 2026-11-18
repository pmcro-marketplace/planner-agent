using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

const string AgentName = "planner-agent";
const string A2APath = "/a2a/planner";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeyedSingleton<AIAgent>(AgentName, (services, _) =>
    new ChatClientAgent(
        new TransportProofChatClient(),
        instructions:
            "You are the PMCR-O Planner transport proof. " +
            "Do not claim planner execution. Return the deterministic provider disposition.",
        name: AgentName,
        description: "Produces bounded plans and observable acceptance criteria without performing Make work. CI transport proof uses a non-executing deterministic provider.",
        services: services));

builder.AddA2AServer(AgentName);

var app = builder.Build();
var publicBaseUrl = builder.Configuration["A2A_PUBLIC_BASE_URL"]?.TrimEnd('/') ?? "http://127.0.0.1:5092";

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "PASS",
    agent = AgentName,
    transport = "A2A",
    provider = "transport-proof",
    execution = "NOT_INVOKED"
}));

app.MapA2AHttpJson(AgentName, A2APath);
app.MapA2AJsonRpc(AgentName, A2APath);

app.MapWellKnownAgentCard(new AgentCard
{
    Name = "PMCR-O Planner Agent",
    Description = "Produces bounded plans and observable acceptance criteria without performing Make work.",
    Version = "1.0.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url = publicBaseUrl + A2APath,
            ProtocolBinding = ProtocolBindingNames.HttpJson,
            ProtocolVersion = "1.0",
        }
    ]
});

app.Run();

internal sealed class TransportProofChatClient : IChatClient
{
    private const string Disposition =
        "NOT_INVOKED: A2A transport reached planner-agent; " +
        "the real PMCR-O Planner reasoning/provider is not connected in this CI proof.";

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Disposition)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, Disposition);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
