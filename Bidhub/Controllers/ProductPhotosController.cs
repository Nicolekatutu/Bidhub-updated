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
        public async Task<IActionResult> Upload(ProductPhotosDTO model)
        {
            // Validate that the file exists
            if (model.PhotoUrl == null || model.PhotoUrl.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file extension
            List<string> validExtensions = new List<string> { ".jpg", ".png" };
            string extension = Path.GetExtension(model.PhotoUrl.FileName).ToLower();
            if (!validExtensions.Contains(extension))
            {
                return BadRequest($"Invalid file extension. Allowed extensions: {string.Join(", ", validExtensions)}");
            }

            // Validate file size
            long size = model.PhotoUrl.Length;
            if (size > (5 * 1024 * 1024)) // 5 MB
            {
                return BadRequest("File size cannot exceed 5MB.");
            }

            // Define upload directory
            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }

            // Create a unique file name
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.PhotoUrl.FileName);
            var filePath = Path.Combine(uploadsDirectory, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.PhotoUrl.CopyToAsync(stream);
                }

                // Save the file information to the database
                var productPic = new ProductPhoto
                {
                    PhotoUrl = $"/Uploads/{fileName}", // Relative path to access the file
                    ProductId = model.ProductId // Assuming this matches an existing product
                };
                _userContext.ProductPhotos.Add(productPic);
                await _userContext.SaveChangesAsync();

                return Ok(new { FileUrl = $"/Uploads/{fileName}" });
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 status
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