using ArWoh.API.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IBlobService _blobService;

    public FileController(IBlobService blobService)
    {
        _blobService = blobService;
    }


    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is not valid");

        try
        {
            using (var stream = file.OpenReadStream())
            {
                await _blobService.UploadFileAsync(file.FileName, stream);
            }

            var filePreviewUrl = await _blobService.GetPreviewUrlAsync(file.FileName);

            return Ok(new
            {
                message = "Upload successfully",
                fileName = file.FileName,
                previewUrl = filePreviewUrl // Trả về link preview MinIO UI
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

}