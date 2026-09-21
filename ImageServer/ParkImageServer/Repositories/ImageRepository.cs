using ParkImageServer.Models;

namespace ParkImageServer.Repositories;

public class ImageRepository
{
    private readonly string _rootPath;

    public ImageRepository(IConfiguration config)
    {
        _rootPath = config["ImageServer:RootPath"] ?? "D:\\Image";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ImageInfo> SaveFileAsync(
        IFormFile file,
        string fileName,
        CancellationToken cancellationToken)
    {
        ImageFileName parsed = Parse(fileName);
        string directory = GetDatePath(parsed);
        Directory.CreateDirectory(directory);
        string savePath = Path.Combine(directory, parsed.FileName);

        using (FileStream stream = new(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        FileInfo saved = new(savePath);
        if (!saved.Exists || saved.Length <= 0)
            throw new IOException("image save failed");
        return new ImageInfo { FileName = parsed.FileName, FullPath = savePath };
    }

    public bool ExistsFile(string fileName)
    {
        ImageFileName parsed = Parse(fileName);
        return File.Exists(Path.Combine(GetDatePath(parsed), parsed.FileName));
    }

    public ImageInfo? GetFile(string fileName)
    {
        if (!ImageFileName.TryParse(fileName, out ImageFileName? parsed)) return null;
        string fullPath = Path.Combine(GetDatePath(parsed!), parsed!.FileName);
        return File.Exists(fullPath)
            ? new ImageInfo { FileName = parsed.FileName, FullPath = fullPath }
            : null;
    }

    private string GetDatePath(ImageFileName value) => Path.Combine(
        _rootPath,
        value.CaptureAt.ToString("yyyy"),
        value.CaptureAt.ToString("MM"),
        value.CaptureAt.ToString("dd"));

    private static ImageFileName Parse(string fileName) =>
        ImageFileName.TryParse(fileName, out ImageFileName? result)
            ? result!
            : throw new ArgumentException("잘못된 이미지 파일명입니다.", nameof(fileName));
}
