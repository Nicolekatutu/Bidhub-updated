using Bidhub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using Bidhub.Dto;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Bidhub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   

    public class AuthController : ControllerBase
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly UserContext _userContext;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;




        public AuthController(UserContext userContext, UserManager<User> userManager, IConfiguration config, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _config = config;
            _signInManager = signInManager;
            _userContext = userContext;
            _roleManager = roleManager;
        }

        [HttpPost("register-bidder")]
        public async Task<IActionResult> RegisterBidder([FromBody] BidderRegisterDto model)
        {
           
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            
            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Password and Confirm Password do not match." });
            }

            
            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email,
                Password = model.Password,  
                PhysicalAddress = model.PhysicalAddress,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            
            var bidder = new Bidders
            {
                UserId = user.Id,
                CompanyUrl = model.CompanyUrl
            };

            _userContext.Bidders.Add(bidder);
            await _userContext.SaveChangesAsync();

            var otp = GenerateOTP();

            var otpEntry = new UserOtp
            {
                UserId = user.Id,
                OtpCode = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(15)
            };

            _userContext.UserOtps.Add(otpEntry);
            await _userContext.SaveChangesAsync();

            // Send OTP via email
            var emailSent = await SendOtpByEmail(user.Email, otp);
            if (!emailSent)
            {
                return StatusCode(500, new { message = "Failed to send OTP email" });
            }

            return Ok(new { message = "Bidder Registered successfully" });
        }

        [HttpPost("register-auctioneer")]
        public async Task<IActionResult> RegisterAuctioneer([FromBody] AuctioneerRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Step 1: Find the company by name or create a new one if it doesn't exist
            var company = await _userContext.Companies
                .FirstOrDefaultAsync(c => c.CompanyName == model.CompanyName);

            if (company == null)
            {
                company = new Company
                {
                    CompanyName = model.CompanyName
                };
                _userContext.Companies.Add(company);
                await _userContext.SaveChangesAsync();
            }

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.UserName,
                Email = model.Email,
                Password = model.Password,
                PhysicalAddress = model.PhysicalAddress,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var role = await _roleManager.FindByNameAsync(model.Role);
            if (role == null)
            {
                return BadRequest(new { message = "Specified role does not exist" });
            }

            var auctioneer = new Auctioneer
            {
                UserId = user.Id,
                RoleId = role.Id,
                StaffNo = model.StaffNo,
                CompanyId = company.CompanyId
            };
            _userContext.Auctioneers.Add(auctioneer);
            await _userContext.SaveChangesAsync();

            return Ok("Auctioneer registered successfully");

        }

        //[Authorize]
        //[HttpPost("verify-otp")]

        //public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        //{
        //    if (string.IsNullOrEmpty(dto?.Otp))
        //    {
        //        return BadRequest(new { message = "The OTP field is required." });
        //    }

        //    var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        //    if (string.IsNullOrEmpty(email))
        //    {
        //        return Unauthorized(new { message = "Invalid token. Email not found." });
        //    }


        //    var user = await _userContext.Users.SingleOrDefaultAsync(u => u.Email == email);
        //    if (user == null)
        //    {
        //        return NotFound(new { message = "User not found." });
        //    }


        //    var otpEntry = await _userContext.UserOtps
        //        .Where(o => o.UserId == user.UserId && o.OtpCode == dto.Otp)
        //        .OrderByDescending(o => o.ExpiryTime)
        //        .FirstOrDefaultAsync();

        //    if (otpEntry == null || otpEntry.ExpiryTime < DateTime.UtcNow)
        //    {
        //        return BadRequest(new { message = "Invalid or expired OTP." });
        //    }

        //    // Mark the user as verified
        //    user.IsVerified = true;
        //    var result = await _userManager.UpdateAsync(user);

        //    if (!result.Succeeded)
        //    {
        //        return StatusCode(500, new { message = "Failed to update user verification status." });
        //    }

        //    return Ok(new { message = "User verified successfully." });
        //}



        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Otp))
            {
                return BadRequest(new { message = "Email and OTP are required." });
            }

            var user = await _userContext.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var otpEntry = await _userContext.UserOtps
                .Where(o => o.UserId == user.Id && o.OtpCode == dto.Otp)
                .OrderByDescending(o => o.ExpiryTime)
                .FirstOrDefaultAsync();

            if (otpEntry == null || otpEntry.ExpiryTime < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            // OTP is valid, mark user as verified
            user.IsVerified = true;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return StatusCode(500, new { message = "Failed to update user verification status." });
            }

            return Ok(new { message = "User verified successfully." });
        }



        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task<bool> SendOtpByEmail(string email, string otp)
        {
            try
            {
                var fromAddress = new MailAddress(_config["EmailSettings:FromEmail"], "Bidhub");
                var toAddress = new MailAddress(email);
                string fromPassword = _config["EmailSettings:EmailPassword"];
                string subject = "Your OTP Code";
                string body = $"Your OTP code is: {otp}";

                var smtp = new SmtpClient
                {
                    Host = _config["EmailSettings:SmtpHost"],
                    Port = int.Parse(_config["EmailSettings:SmtpPort"]),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    await smtp.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto userDto)
        {
            var user = await _userContext.Users.SingleOrDefaultAsync(u => u.UserName == userDto.UserName);
            if (user == null)
                return Unauthorized();


            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        //    private string GenerateJwtToken(User user)
        //    {
        //        var claims = new[]
        //        {
        //    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //    new Claim(ClaimTypes.Email, user.Email)
        //};

        //        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        //        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //        var token = new JwtSecurityToken(
        //            issuer: _config["Jwt:Issuer"],
        //            audience: _config["Jwt:Audience"],
        //            claims: claims,
        //            expires: DateTime.Now.AddMinutes(60),
        //            signingCredentials: creds);

        //        return new JwtSecurityTokenHandler().WriteToken(token);
        //    }



        //    private string GenerateJwtToken(User user)
        //    {
        //        // Retrieve AuctioneerId associated with the user, if it exists
        //        var auctioneer = _userContext.Auctioneers.FirstOrDefault(a => a.UserId == user.UserId);
        //        var auctioneerId = auctioneer?.AuctioneerId.ToString() ?? string.Empty;

        //        var claims = new[]
        //        {
        //    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        //    new Claim(ClaimTypes.Email, user.Email),
        //    new Claim("AuctioneerId", auctioneerId) 
        //};

        //        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        //        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //        var token = new JwtSecurityToken(
        //            issuer: _config["Jwt:Issuer"],
        //            audience: _config["Jwt:Audience"],
        //            claims: claims,
        //            expires: DateTime.Now.AddMinutes(60),
        //            signingCredentials: creds);

        //        return new JwtSecurityTokenHandler().WriteToken(token);
        //    }

        private string GenerateJwtToken(User user)
        {
            
            var auctioneer = _userContext.Auctioneers.FirstOrDefault(a => a.UserId == user.Id);

            var auctioneerId = auctioneer?.AuctioneerId.ToString() ?? string.Empty;

            // Define claims for the JWT token
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserName), 
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email), 
        new Claim("AuctioneerId", auctioneerId) 
    };

           
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the JWT token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], 
                audience: _config["Jwt:Audience"], 
                claims: claims, 
                expires: DateTime.Now.AddMinutes(60), 
                signingCredentials: creds 
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
