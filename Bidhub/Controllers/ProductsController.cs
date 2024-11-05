using Bidhub.Models;
using Bidhub.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly UserContext _userContext;

    public ProductsController(UserContext context)
    {
        _userContext = context;
    }


    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductsDTO productDTO)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = new Product
        {
            ProductName = productDTO.ProductName,
            ReasonForAuction = productDTO.ReasonForAuction,
            OwnerName = productDTO.OwnersName,
            OwnerPhoneNo = productDTO.OwnerPhoneNo,
            ReservePrice = productDTO.ReservePrice,
            Location = productDTO.Location, 
        };

        
        _userContext.Products.Add(product);
        await _userContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    // GET: api/Auctioneers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductsDTO>>> GetProduct()
    {
        var product = await _userContext.Products
            .Select(a => new ProductsDTO
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
    public async Task<ActionResult<ProductsDTO>> GetProduct(int id)
    {
        var product = await _userContext.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        var productDTO = new ProductsDTO
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
    public async Task<IActionResult> PutProduct(int id, ProductsDTO productDTO)
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