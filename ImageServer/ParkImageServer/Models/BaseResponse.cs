namespace ParkImageServer.Models
{
    public class BaseResponse
    {
        public int Result { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
