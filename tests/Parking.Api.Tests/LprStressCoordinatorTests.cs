using System.Net;
using System.Net.Sockets;
using Parking.Contracts;
using Parking.EdgeService;
using Parking.LprStressTester;

namespace Parking.Api.Tests;

public sealed class LprStressCoordinatorTests
{
    [Fact]
    public void 생성한_파일명은_EdgeService_규칙으로_해석된다()
    {
        LprStressOptions options = LprStressOptions.Parse(Array.Empty<string>());
        string fileName = LprFileNameFactory.Create(options, 104, 20, new DateTime(2026, 9, 20, 12, 30, 45, 123));

        LprParseResult result = new LprFileNameParser().Parse(fileName);

        Assert.True(result.Success);
        Assert.Equal(9001, result.Recognition!.SiteId);
        Assert.Equal(2, result.Recognition.Groupnum);
        Assert.Equal(104, result.Recognition.DeviceId);
        Assert.Equal(9010, result.Recognition.LaneId);
    }

    [Fact]
    public async Task 네_연결에서_연속요청을_모두_ACK하면_성공한다()
    {
        await using AckServer server = new(expectedClients: 4, expectedFramesPerClient: 3, splitReplies: true);
        LprStressOptions options = new("127.0.0.1", server.Port, 9001, 2, 9010,
            new[] { 101, 102, 103, 104 }, 3, 0, 3);

        LprStressResult result = await new LprStressCoordinator().RunAsync(options, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(12, result.Sent);
        Assert.Equal(12, result.Acknowledged);
        Assert.All(result.Cameras, camera => Assert.Equal(3, camera.Acknowledged));
    }

    [Fact]
    public async Task 서버가_응답전에_연결을_닫으면_실패한다()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = Task.Run(async () => (await listener.AcceptTcpClientAsync()).Dispose());
        LprStressOptions options = new("127.0.0.1", port, 9001, 2, 9010,
            new[] { 101 }, 1, 0, 2);

        LprStressResult result = await new LprStressCoordinator().RunAsync(options, CancellationToken.None);

        listener.Stop();
        await server;
        Assert.False(result.Success);
        Assert.Equal(1, result.ConnectionErrors);
    }

    [Fact]
    public void 실패집계가_하나라도_있으면_종료코드_2이다()
    {
        LprStressResult success = new(new[] { new LprCameraResult(101, 1, 1, 0, 0, 0, 0, null) });
        LprStressResult failure = new(new[] { new LprCameraResult(101, 1, 0, 1, 0, 0, 0, "NAK") });

        Assert.Equal(0, LprStressExitCode.FromResult(success));
        Assert.Equal(2, LprStressExitCode.FromResult(failure));
    }

    private sealed class AckServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _runTask;
        private readonly int _expectedClients;
        private readonly int _expectedFramesPerClient;
        private readonly bool _splitReplies;

        public AckServer(int expectedClients, int expectedFramesPerClient, bool splitReplies)
        {
            _expectedClients = expectedClients;
            _expectedFramesPerClient = expectedFramesPerClient;
            _splitReplies = splitReplies;
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _runTask = RunAsync();
        }

        public int Port { get; }

        private async Task RunAsync()
        {
            List<Task> clients = new();
            for (int index = 0; index < _expectedClients; index++)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cancellation.Token);
                clients.Add(HandleAsync(client));
            }
            await Task.WhenAll(clients);
        }

        private async Task HandleAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[512];
                int frames = 0;
                bool receiving = false;
                while (frames < _expectedFramesPerClient)
                {
                    int count = await stream.ReadAsync(buffer, _cancellation.Token);
                    if (count == 0) return;
                    for (int index = 0; index < count; index++)
                    {
                        if (buffer[index] == 0x02) receiving = true;
                        else if (receiving && buffer[index] == 0x03)
                        {
                            receiving = false;
                            frames++;
                            byte[] reply = Frame($"ACK|{Guid.NewGuid():N}");
                            if (_splitReplies)
                            {
                                await stream.WriteAsync(reply.AsMemory(0, 1), _cancellation.Token);
                                await stream.WriteAsync(reply.AsMemory(1), _cancellation.Token);
                            }
                            else await stream.WriteAsync(reply, _cancellation.Token);
                        }
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try { await _runTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            finally
            {
                _cancellation.Cancel();
                _listener.Stop();
                _cancellation.Dispose();
            }
        }

        private static byte[] Frame(string value) =>
            new byte[] { 0x02 }.Concat(System.Text.Encoding.ASCII.GetBytes(value)).Append((byte)0x03).ToArray();
    }
}
