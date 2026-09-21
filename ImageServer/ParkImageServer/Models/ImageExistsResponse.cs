namespace ParkImageServer.Models
{
    public class ImageExistsResponse : BaseResponse
    {
        public bool Exists { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
