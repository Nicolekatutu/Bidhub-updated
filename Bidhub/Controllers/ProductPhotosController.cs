using Bidhub.Dto;
using Bidhub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Bidhub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductPhotosController : ControllerBase
    {
        private readonly UserContext _userContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductPhotosController(UserContext Usercontext, IWebHostEnvironment webHostEnvironment)
        {
            _userContext = Usercontext;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("images")] // api/main/uploadFile
        //public IActionResult Upload(IFormFile file)
        public IActionResult Upload(ProductPhotosDTO productpic)
        {
            // Validate file extension
            List<string> validExtensions = new List<string> { ".jpg", ".png" };
            string extension = Path.GetExtension(productpic.PhotoUrl.FileName);
            if (!validExtensions.Contains(extension))
            {
                return BadRequest($"Extension is not valid ({string.Join(", ", validExtensions)})");
            }



            // Validate file size
            long size = productpic.PhotoUrl.Length;
            if (size > (5 * 1024 * 1024))
            {
                return BadRequest("Maximum size can be 5MB");
            }

            // Ensure WebRootPath is not null
            if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
            {
                return StatusCode(500, "Web root path is not configured.");
            }

            // Generate a new filename
            string fileName = Guid.NewGuid().ToString() + extension;
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads/images");

            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = Path.Combine(uploadDir, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    productpic.PhotoUrl.CopyTo(stream);
                }

                // Generate the URL for the uploaded file
                string fileUrl = $"{Request.Scheme}://{Request.Host}/Uploads/images/{fileName}";

                // Save the file information to the database
                var productPic = new ProductPhoto
                {
                    PhotoUrl = fileUrl,
                    ProductId = productpic.ProductId // This should now exist in the Products table
                };
                _userContext.ProductPhotos.Add(productPic);
                _userContext.SaveChanges();

                return Ok(new { fileUrl });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while uploading the file.");
            }
        }
        // GET: api/Auctioneers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductPhotosDTO>>> Getproductpic()
        {
            var productPic = await _userContext.ProductPhotos
                .Select(a => new ProductPhotosDTO
                {
                    ProductId = a.ProductId
                })
                .ToListAsync();

            return Ok(productPic);
        }

        // GET: api/Auctioneers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductRtnPhotoDTO>> Getproductpic(int id)
        {
            var productPic = await _userContext.ProductPhotos.FindAsync(id);

            if (productPic == null)
            {
                return NotFound();
            }

            var ProductrtnPhotoDTO = new ProductRtnPhotoDTO
            {

                ProductId = productPic.ProductId,
                PhotoUrl = productPic.PhotoUrl

            };

            return Ok(ProductrtnPhotoDTO);
        }

        // PUT: api/Auctioneers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProductImg(int id, ProductPhotosDTO productPicDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var productPic = await _userContext.ProductPhotos.FindAsync(id);
            if (productPic == null)
            {
                return NotFound();
            }

            productPic.ProductId = productPicDTO.ProductId;

            _userContext.Entry(productPic).State = EntityState.Modified;

            try
            {
                await _userContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_userContext.ProductPhotos.Any(e => e.PhotoId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Auctioneers/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProductImg(int id)
        {
            var productPic = await _userContext.ProductPhotos.FindAsync(id);

            if (productPic == null)
            {
                return NotFound();
            }

            _userContext.ProductPhotos.Remove(productPic);
            await _userContext.SaveChangesAsync();


            return NoContent();
        }


    }
}