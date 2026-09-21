using ParkImageServer.Models;
using ParkImageServer.Repositories;

namespace ParkImageServer.Services;

public sealed class ImageService
{
    private readonly ImageRepository _repository;
    public ImageService(IConfiguration config) => _repository = new ImageRepository(config);

    public async Task<ImageUploadResponse> UploadAsync(
        IFormFile? file,
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return new ImageUploadResponse { Result = -1, Message = "파일 없음" };
        string name = string.IsNullOrWhiteSpace(fileName) ? file.FileName : fileName;
        ImageInfo info = await _repository.SaveFileAsync(file, name, cancellationToken);
        return new ImageUploadResponse
        {
            Result = 0,
            Message = "정상처리",
            FileName = info.FileName,
            FilePath = ""
        };
    }

    public ImageExistsResponse Exists(string fileName)
    {
        bool exists = _repository.ExistsFile(fileName);
        return new ImageExistsResponse
        {
            Result = 0,
            Message = "정상처리",
            Exists = exists,
            FileName = fileName
        };
    }

    public ImageInfo? GetFile(string fileName) => _repository.GetFile(fileName);
}
