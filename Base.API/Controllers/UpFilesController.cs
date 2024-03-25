using Base.Service.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpFilesController : ControllerBase
    {
        private readonly IUploadFile upload;
        public UpFilesController(IUploadFile uploadFile)
        {
            upload = uploadFile;
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile? file)
        {
            if(file is not null && file.Length > 0)
            {
                var uploadResult = await upload.UploadImage(file);
                if (uploadResult.Error is not null)
                {
                    return BadRequest();
                }
                return Ok(uploadResult.SecureUrl.ToString() + "   " + uploadResult.Url.ToString());
            }
            return BadRequest();
        }
    }
}
