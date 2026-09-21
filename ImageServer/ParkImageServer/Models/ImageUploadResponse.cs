namespace ParkImageServer.Models
{
    public class ImageUploadResponse : BaseResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
