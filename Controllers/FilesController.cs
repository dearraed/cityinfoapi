using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace CityInfo.API.Controllers
{
    [Route("api/v{version:apiVersion}/files")]
    [ApiController]
    //[Authorize]
    public class FilesController : ControllerBase
    {
        private readonly FileExtensionContentTypeProvider _fileExtensioncontentTypeProvider;

        public FilesController(FileExtensionContentTypeProvider fileExtensioncontentTypeProvider)
        {
            _fileExtensioncontentTypeProvider = fileExtensioncontentTypeProvider ?? throw new System.ArgumentNullException(nameof(fileExtensioncontentTypeProvider));
        }

        [HttpGet("{fileId}")]
        [ApiVersion("0.1", Deprecated = true)]
        public ActionResult GetFiles()
        {
            var pathToFile = "getting-started-with-rest-slides.pdf";

            if(!System.IO.File.Exists(pathToFile))
            {
                return NotFound();
            }   

            if(!_fileExtensioncontentTypeProvider.TryGetContentType(pathToFile, out var contentType))
            {
                contentType = "application/octet-stream";
            }   

            var bytes = System.IO.File.ReadAllBytes(pathToFile);
            return File(bytes, contentType, Path.GetFileName(pathToFile));
        }

        [HttpPost]
        public async Task<ActionResult> CreateFile(IFormFile file)
        {
            if (file.Length == 0 || file.Length > 20971520 || file.ContentType != "application/pdf")
            {
                return BadRequest("No file or an invalid one has been inputted.");
            }
            var path = Path.Combine(Directory.GetCurrentDirectory(), $"uploaded_file_{Guid.NewGuid()}.pdf");
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return Ok("fichier uploadé avec succès");
        }
    }
}
