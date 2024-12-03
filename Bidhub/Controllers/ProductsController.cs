using Bidhub.Models;
using Bidhub.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Bidhub.Controllers 
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly UserContext _userContext;

        public ProductsController(UserContext usercontext)
        {
            _userContext = usercontext;
        }

        [Authorize]
        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductsDto productsDto)
        {
            // Automatic model validation via [ApiController]
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Retrieve the AuctioneerId from JWT claims
            var auctioneerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "AuctioneerId");
            if (auctioneerIdClaim == null)
                return Unauthorized(new { message = "Auctioneer ID is missing in token." });

            if (!int.TryParse(auctioneerIdClaim.Value, out int auctioneerId))
                return BadRequest(new { message = "Invalid Auctioneer ID format." });

          
            var auctioneerExists = await _userContext.Auctioneers.AnyAsync(a => a.AuctioneerId == auctioneerId);
            if (!auctioneerExists)
                return BadRequest(new { message = "Auctioneer not found." });

            
            var product = new Product
            {
                ProductName = productsDto.ProductName,
                ReasonForAuction = productsDto.ReasonForAuction,
                OwnerName = productsDto.OwnersName,
                OwnerPhoneNo = productsDto.OwnerPhoneNo,
                ReservePrice = productsDto.ReservePrice,
                Location = productsDto.Location,
                AuctioneerId = auctioneerId 
            };

            _userContext.Products.Add(product);
            await _userContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }

        // GET: api/Auctioneers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductsDto>>> GetProduct()
        {
            var product = await _userContext.Products
                .Select(a => new ProductsDto
                {
                    ProductName = a.ProductName,
                    ReasonForAuction = a.ReasonForAuction,
                    OwnersName = a.OwnerName,
                    OwnerPhoneNo = a.OwnerPhoneNo,
                    ReservePrice = a.ReservePrice,
                    Location = a.Location
                })
                .ToListAsync();

            return Ok(product);
        }

        // GET: api/Auctioneers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductsDto>> GetProduct(int id)
        {
            var product = await _userContext.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var productDTO = new ProductsDto
            {

                ProductName = product.ProductName,
                ReasonForAuction = product.ReasonForAuction,
                OwnersName = product.OwnerName,
                OwnerPhoneNo = product.OwnerPhoneNo,
                ReservePrice = product.ReservePrice,
                Location = product.Location
            };

            return Ok(productDTO);
        }

        // PUT: api/Auctioneers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, ProductsDto productDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var products = await _userContext.Products.FindAsync(id);
            if (products == null)
            {
                return NotFound();
            }

            products.ProductName = productDTO.ProductName;
            products.ReasonForAuction = productDTO.ReasonForAuction;
            products.OwnerName = productDTO.OwnersName;
            products.OwnerPhoneNo = productDTO.OwnerPhoneNo;
            products.ReservePrice = productDTO.ReservePrice;
            products.Location = productDTO.Location;

            _userContext.Entry(products).State = EntityState.Modified;

            try
            {
                await _userContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_userContext.Products.Any(e => e.ProductId == id))
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
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var products = await _userContext.Products.FindAsync(id);
            if (products == null)
            {
                return NotFound();
            }

            _userContext.Products.Remove(products);
            await _userContext.SaveChangesAsync();

            return NoContent();
        }

    }


}