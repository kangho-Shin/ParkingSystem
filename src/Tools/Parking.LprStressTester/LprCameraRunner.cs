using System.Net.Sockets;

namespace Parking.LprStressTester;

public sealed record LprCameraResult(
    int DeviceNumber,
    int Sent,
    int Acknowledged,
    int Nak,
    int Timeouts,
    int ProtocolErrors,
    int ConnectionErrors,
    string? LastError)
{
    public bool Success => Sent == Acknowledged && Nak == 0 && Timeouts == 0 && ProtocolErrors == 0 && ConnectionErrors == 0;
}

public sealed class LprCameraRunner
{
    public async Task<LprCameraResult> RunAsync(
        LprStressOptions options,
        int deviceNumber,
        CancellationToken cancellationToken)
    {
        int sent = 0, acknowledged = 0, nak = 0, timeouts = 0, protocolErrors = 0, connectionErrors = 0;
        string? lastError = null;
        using TcpClient client = new();
        try
        {
            using (CancellationTokenSource timeout = CreateTimeout(options, cancellationToken))
                await client.ConnectAsync(options.Host, options.Port, timeout.Token);

            NetworkStream stream = client.GetStream();
            LprReplyParser parser = new();
            Queue<LprReply> pendingReplies = new();
            for (int sequence = 1; sequence <= options.Count; sequence++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string fileName = LprFileNameFactory.Create(options, deviceNumber, sequence, DateTime.Now);
                byte[] packet = LprWireProtocol.EncodeRequest(fileName);
                sent++;
                using CancellationTokenSource timeout = CreateTimeout(options, cancellationToken);
                await stream.WriteAsync(packet, timeout.Token);
                LprReply reply = await ReadReplyAsync(stream, parser, pendingReplies, timeout.Token);
                if (!reply.Valid)
                {
                    protocolErrors++;
                    lastError = reply.ErrorCode;
                }
                else if (!reply.Acknowledged)
                {
                    nak++;
                    lastError = reply.ErrorCode;
                }
                else if (reply.EventId is null)
                {
                    protocolErrors++;
                    lastError = "ACK_EVENT_ID_MISSING";
                }
                else acknowledged++;

                if (!reply.Valid || !reply.Acknowledged) break;
                if (options.IntervalMilliseconds > 0)
                    await Task.Delay(options.IntervalMilliseconds, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timeouts++;
            lastError = "TIMEOUT";
        }
        catch (OperationCanceledException)
        {
            connectionErrors++;
            lastError = "CANCELLED";
        }
        catch (Exception exception) when (exception is IOException or SocketException)
        {
            connectionErrors++;
            lastError = exception.Message;
        }

        return new(deviceNumber, sent, acknowledged, nak, timeouts, protocolErrors, connectionErrors, lastError);
    }

    private static async Task<LprReply> ReadReplyAsync(
        NetworkStream stream,
        LprReplyParser parser,
        Queue<LprReply> pendingReplies,
        CancellationToken token)
    {
        if (pendingReplies.Count > 0) return pendingReplies.Dequeue();
        byte[] buffer = new byte[512];
        while (true)
        {
            int count = await stream.ReadAsync(buffer, token);
            if (count == 0) throw new IOException("서버가 응답 전에 연결을 종료했습니다.");
            foreach (LprReply reply in parser.Append(buffer.AsSpan(0, count)))
                pendingReplies.Enqueue(reply);
            if (pendingReplies.Count > 0) return pendingReplies.Dequeue();
        }
    }

    private static CancellationTokenSource CreateTimeout(LprStressOptions options, CancellationToken token)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(token);
        source.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        return source;
    }
}
