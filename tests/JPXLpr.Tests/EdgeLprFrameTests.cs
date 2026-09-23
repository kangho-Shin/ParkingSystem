using System.Text;
using JPXLpr.Edge;

namespace JPXLpr.Tests;

public sealed class EdgeLprFrameTests
{
    [Theory]
    [InlineData(0, 2, 9010, 401, "Entry")]
    [InlineData(9001, 0, 9010, 401, "Entry")]
    [InlineData(9001, 2, 0, 401, "Entry")]
    [InlineData(9001, 2, 9010, 0, "Entry")]
    [InlineData(9001, 1000, 9010, 401, "Entry")]
    [InlineData(9001, 2, 9010, 1000, "Entry")]
    [InlineData(9001, 2, 9010, 401, "Unknown")]
    public void Invalid_identity_is_rejected(int site, int group, int lane, int device, string direction)
    {
        EdgeLprOptions value = new(site, group, lane, device, direction, "127.0.0.1", 29200);
        Assert.Throws<InvalidOperationException>(() => value.Validate());
    }

    [Fact]
    public void Entry_filename_and_cp949_frame_match_edge_protocol()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EdgeLprEvent value = EdgeLprEvent.Create(9001, 2, 401, 9010, "Entry",
            new DateTime(2026, 9, 21, 10, 20, 30, 456), "12가3456", id);

        Assert.Equal("9001_002_401_9010_Entry_20260921102030456_12가3456_11111111222233334444555555555555.jpg", value.FileName);
        byte[] frame = EdgeLprFrame.Encode(value.FileName);
        Assert.Equal(0x02, frame[0]);
        Assert.Equal(0x03, frame[^1]);
        Assert.Equal(value.FileName, Encoding.GetEncoding(949).GetString(frame[1..^1]));
    }

    [Fact]
    public void Exit_filename_uses_exit_identity()
    {
        EdgeLprEvent value = EdgeLprEvent.Create(9001, 2, 402, 9020, "Exit",
            new DateTime(2026, 9, 21, 11, 22, 33, 789), "34나5678",
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        Assert.StartsWith("9001_002_402_9020_Exit_20260921112233789_34나5678_", value.FileName);
    }

    [Fact]
    public void Framed_ack_with_compact_event_id_is_accepted()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}ACK|{id:N}{(char)0x03}", id);
        Assert.True(result.Accepted);
    }

    [Fact]
    public void Hyphenated_ack_event_id_is_rejected()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}ACK|{id:D}{(char)0x03}", id);

        Assert.False(result.Accepted);
        Assert.Equal("INVALID_RESPONSE", result.Code);
    }

    [Fact]
    public void Ack_event_id_with_trailing_space_is_rejected()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}ACK|{id:N} {(char)0x03}", id);

        Assert.False(result.Accepted);
        Assert.Equal("INVALID_RESPONSE", result.Code);
    }

    [Fact]
    public void Framed_nak_is_rejected()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}NAK|{id:N}|DEVICE_NOT_FOUND{(char)0x03}", id);
        Assert.False(result.Accepted);
        Assert.Equal("NAK", result.Code);
    }

    [Fact]
    public void Nak_with_different_event_id_is_not_matched_to_request()
    {
        Guid expected = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid actual = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}NAK|{actual:N}|INVALID_DEVICE{(char)0x03}", expected);

        Assert.False(result.Accepted);
        Assert.Equal("RESPONSE_EVENT_MISMATCH", result.Code);
    }

    [Theory]
    [InlineData("PAYMENT_REQUIRED")]
    [InlineData("OPEN_SESSION_NOT_FOUND")]
    public void Completed_business_nak_is_not_retried(string errorCode)
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}NAK|{id:N}|{errorCode}{(char)0x03}", id);

        Assert.True(EdgeLprResponseParser.IsPermanentFailure(result));
    }

    [Theory]
    [InlineData("PROCESSING_ERROR")]
    [InlineData("CONFIG_NOT_READY")]
    [InlineData("CENTRAL_OFFLINE_EXIT_BLOCKED")]
    [InlineData("KIOSK_OFFLINE_BLOCKED")]
    public void Temporary_nak_is_retried(string errorCode)
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}NAK|{id:N}|{errorCode}{(char)0x03}", id);

        Assert.False(EdgeLprResponseParser.IsPermanentFailure(result));
    }

    [Theory]
    [InlineData("NAKX|11111111222233334444555555555555|PAYMENT_REQUIRED")]
    [InlineData("NAK|11111111222233334444555555555555|junk|PAYMENT_REQUIRED")]
    [InlineData("NAK||INVALID_ENCODING")]
    public void Malformed_nak_never_completes_outbox_item(string payload)
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        EdgeLprSendResult result = EdgeLprResponseParser.Match(
            $"{(char)0x02}{payload}{(char)0x03}", id);

        Assert.Equal("INVALID_RESPONSE", result.Code);
        Assert.False(EdgeLprResponseParser.IsPermanentFailure(result));
    }

    [Fact]
    public void Car_number_whitespace_is_removed_for_edge_parser()
    {
        EdgeLprEvent value = EdgeLprEvent.Create(9001, 2, 401, 9010, "Entry",
            DateTime.Now, "12가 3456", Guid.Empty);
        Assert.Equal("12가3456", value.CarNumber);
    }
}
