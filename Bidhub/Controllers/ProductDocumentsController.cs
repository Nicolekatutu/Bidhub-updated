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
        public IActionResult Upload(ProductDocDTO productdoc, string fileName)
        {
            // Validate file extension
            List<string> validExtensions = new List<string> { ".pdf", ".ppt" };
            string extension = Path.GetExtension(productdoc.DocumentUrl.FileName);
            if (!validExtensions.Contains(extension))
            {
                return BadRequest($"Extension is not valid ({string.Join(", ", validExtensions)})");
            }

            if (productdoc.DocumentUrl == null || string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File or file name is missing.");
            }

            // Validate file size
            long size = productdoc.DocumentUrl.Length;
            if (size > (5 * 1024 * 1024))
            {
                return BadRequest("Maximum size can be 5MB");
            }

            // Ensure WebRootPath is not null
            if (string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
            {
                return StatusCode(500, "Web root path is not configured.");
            }

            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads/documents");

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
                    productdoc.DocumentUrl.CopyTo(stream);
                }

                // Generate the URL for the uploaded file
                string fileUrl = $"{Request.Scheme}://{Request.Host}/Uploads/documents/{fileName}";

                // Save the file information to the database
                var productDoc = new ProductDocument
                {
                    DocumentType = fileName,
                    DocumentUrl = fileUrl,
                    ProductId = productdoc.ProductId // This should now exist in the Products table

                };
                _userContext.ProductDocuments.Add(productDoc);
                _userContext.SaveChanges();

                return Ok(new { fileName, fileUrl });
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