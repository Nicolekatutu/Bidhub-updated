using Bidhub.Dto;
using Bidhub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Xml.Linq;

namespace Bidhub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductDocumentsController : ControllerBase
    {
        private readonly UserContext _userContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductDocumentsController(UserContext UserContext, IWebHostEnvironment webHostEnvironment)
        {
            _userContext =UserContext;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("documents")] // api/main/uploadFile
        public IActionResult Upload(ProductDocDTO productdoc)
        {
            // Validate the file exists
            if (productdoc.DocumentUrl == null)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file extension
            List<string> validExtensions = new List<string> { ".pdf", ".ppt" };
            string extension = Path.GetExtension(productdoc.DocumentUrl.FileName).ToLower();
            if (!validExtensions.Contains(extension))
            {
                return BadRequest($"Invalid file extension. Allowed extensions: {string.Join(", ", validExtensions)}");
            }

            // Validate file size
            long size = productdoc.DocumentUrl.Length;
            if (size > (5 * 1024 * 1024)) // 5 MB
            {
                return BadRequest("File size cannot exceed 5MB.");
            }

            // Define upload directory
            var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads/documents");
            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }

            // Create a unique file name
            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsDirectory, fileName);

            try
            {
                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    productdoc.DocumentUrl.CopyTo(stream);
                }

                // Save the file information to the database
                var productDoc = new ProductDocument
                {
                    DocumentType = Path.GetFileNameWithoutExtension(fileName), // Optional: Extract name without extension
                    DocumentUrl = $"/Uploads/documents/{fileName}", // Relative path to access the file
                    ProductId = productdoc.ProductId // Assuming this matches an existing product
                };
                _userContext.ProductDocuments.Add(productDoc);
                _userContext.SaveChanges();

                // Return success with file details
                return Ok(new { FileName = fileName, FileUrl = $"/Uploads/documents/{fileName}" });
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
        public async Task<ActionResult<IEnumerable<ProductRtnDocDTO>>> GetProductDoc()
        {
            var productdoc = await _userContext.ProductDocuments
                .Select(a => new ProductRtnDocDTO
                {
                    ProductId = a.ProductId,
                    DocumentUrl = a.DocumentUrl
                })
                .ToListAsync();

            return Ok(productdoc);
        }

        // GET: api/Auctioneers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductRtnDocDTO>> GetProductDoc(int id)
        {
            var productdoc = await _userContext.ProductDocuments.FindAsync(id);

            if (productdoc == null)
            {
                return NotFound();
            }

            var ProductrtnDocDTO = new ProductRtnDocDTO
            {
                DocumentType = productdoc.DocumentType,
                DocumentUrl = productdoc.DocumentUrl,
                ProductId = productdoc.ProductId
            };

            return Ok(ProductrtnDocDTO);
        }

        // PUT: api/Auctioneers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAuctioneer(int id, ProductDocDTO productDocDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var productdoc = await _userContext.ProductDocuments.FindAsync(id);
            if (productdoc == null)
            {
                return NotFound();
            }

            productdoc.ProductId = productDocDTO.ProductId;

            _userContext.Entry(productdoc).State = EntityState.Modified;

            try
            {
                await _userContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_userContext.ProductDocuments.Any(e => e.DocumentId == id))
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
        public async Task<IActionResult> DeleteProductDoc(int id)
        {
            var productdoc = await _userContext.ProductDocuments.FindAsync(id);
            if (productdoc == null)
            {
                return NotFound();
            }

            _userContext.ProductDocuments.Remove(productdoc);
            await _userContext.SaveChangesAsync();

            return NoContent();
        }

    }
}