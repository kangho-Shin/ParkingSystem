namespace ParkImageServer.Models
{
    public class ImageUploadRequest
    {
        public IFormFile File { get; set; }
        public string FileName { get; set; }
    }
}
