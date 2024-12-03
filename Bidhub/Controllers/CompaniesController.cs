using Bidhub.Dto;
using Bidhub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

[Route("api/[controller]")]
[ApiController]
public class CompaniesController : ControllerBase
{
    private readonly UserContext _context;

    public CompaniesController(UserContext context)
    {
        _context = context;
    }


    // POST: api/Companies
    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CompaniesDto companiesDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var company = new Company
        {
            CompanyName = companiesDTO.CompanyName,
            CompanyUrl = companiesDTO.CompanyUrl,
            Location = companiesDTO.Location,
            Status = companiesDTO.Status
            // Assuming the user, company, and product IDs are provided in the request
            //Auctioneers = new List<Auctioneers>()
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCompany), new { id = company.CompanyId }, company);

    }

    // GET: api/Auctioneers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompaniesDto>>> GetCompany()
    {
        var company = await _context.Companies
            .Select(a => new CompaniesDto
            {
                CompanyName = a.CompanyName,
                CompanyUrl = a.CompanyUrl,
                Location = a.Location,
                Status = a.Status

            })
            .ToListAsync();

        return Ok(company);
    }

    // GET: api/Auctioneers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CompaniesDto>> GetCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);

        if (company == null)
        {
            return NotFound();
        }

        var companyDTO = new CompaniesDto
        {

            CompanyName = company.CompanyName,
            CompanyUrl = company.CompanyUrl,
            Location = company.Location,
            Status = company.Status
        };

        return Ok(companyDTO);
    }

    // PUT: api/Auctioneers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCompany(int id, CompaniesDto companyDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        company.CompanyName = companyDTO.CompanyName;
        company.CompanyUrl = companyDTO.CompanyUrl;
        company.Location = companyDTO.Location;
        company.Status = companyDTO.Status;

        _context.Entry(company).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Companies.Any(e => e.CompanyId == id))
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
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        return NoContent();
    }


}