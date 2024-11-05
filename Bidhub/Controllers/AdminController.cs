using Bidhub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bidhub.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace Bidhub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserContext _userContext;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, UserContext userContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userContext = userContext;
        }

        //Create User
        [HttpPost("add-user")]
        public async Task<IActionResult> AddUser([FromForm] AddUserDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var company = await _userContext.Companies
                .FirstOrDefaultAsync(c => c.CompanyUrl == model.CompanyUrl);

            if (company == null)
            {
                company = new Company
                {
                    CompanyUrl = model.CompanyUrl
                };
                _userContext.Companies.Add(company);
                await _userContext.SaveChangesAsync();
            }

            var auctioneer = new Auctioneer
            {
                User = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.FirstName
                },
                StaffNo = model.StaffNo,
                CompanyId = company.CompanyId
            };

            var result = await _userManager.CreateAsync(auctioneer.User, "DefaultPassword123!");
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            if (model.Photo != null && model.Photo.Length > 0)
            {
                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }

                var fileName = Path.GetFileName(model.Photo.FileName);
                var filePath = Path.Combine(uploadsDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                auctioneer.PhotoUrl = $"/Uploads/{fileName}";
                await _userManager.UpdateAsync(auctioneer.User);
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(model.Role));
                }
                await _userManager.AddToRoleAsync(auctioneer.User, model.Role);
            }

            _userContext.Auctioneers.Add(auctioneer);
            await _userContext.SaveChangesAsync();

            return Ok(new { message = "Auctioneer added successfully", auctioneer.User });
        }


        [HttpGet("get-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return Ok(users);
        }

        // Update User
        [HttpPut("update-user/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? Ok(new { message = "User updated successfully", user }) : BadRequest(result.Errors);
        }

        // Delete User
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Ok(new { message = "User deleted successfully" }) : BadRequest(result.Errors);
        }

        // Create Role
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromBody] AddRoleDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
            if (roleExists)
                return BadRequest(new { message = "Role already exists" });

            var role = new IdentityRole<Guid>(model.RoleName);
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Role created successfully", role });
        }


        // Retrieve All Roles
        [HttpGet("get-roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }

        // Retrieve Role by ID
        [HttpGet("get-role/{id}")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            return role == null ? NotFound(new { message = "Role not found" }) : Ok(role);
        }

        // Update Role
        [HttpPut("update-role/{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return NotFound(new { message = "Role not found" });

            role.Name = model.RoleName;
            var result = await _roleManager.UpdateAsync(role);

            return result.Succeeded ? Ok(new { message = "Role updated successfully", role }) : BadRequest(result.Errors);
        }

        // Delete Role
        [HttpDelete("delete-role/{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return NotFound(new { message = "Role not found" });

            var result = await _roleManager.DeleteAsync(role);
            return result.Succeeded ? Ok(new { message = "Role deleted successfully" }) : BadRequest(result.Errors);
        }
    }
}






