namespace ArWoh.API.Interface
{
    public interface IBlobService
    {
        Task UploadFileAsync(string fileName, Stream fileStream);
        Task<string> GetPreviewUrlAsync(string fileName);
        Task<string> GetFileUrlAsync(string fileName);
    }
}
