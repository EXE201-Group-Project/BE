using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrController : ControllerBase
    {
        private static string QrCodeImg = "https://drive.google.com/uc?id=1O37AUeRVyMlQymgWDwlogSRIXkEnm0Q0";
        private static string QrLink = "https://me.momo.vn/8vI1T4tPUOsnu3UOC9FbiQ/jnegpXv3vmK3awZ";

        [HttpGet("QrCodeImg")]
        public IActionResult GetQrCodeImg()
        {
            return Ok(QrCodeImg);
        }

        [HttpGet("QrLink")]
        public IActionResult GetQrLink()
        {
            return Ok(QrLink);
        }

        [HttpPost("QrCodeImg")]
        public IActionResult PostQrCodeImg([FromQuery] string qrCodeImg)
        {
            QrCodeImg = qrCodeImg;
            return Ok();
        }

        [HttpPost("QrLink")]
        public IActionResult PostQrLink([FromQuery] string qrLink)
        {
            QrLink = qrLink;
            return Ok();
        }
    }
}
