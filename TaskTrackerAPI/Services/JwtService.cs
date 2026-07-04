using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskTrackerAPI.Models;

namespace TaskTrackerAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);

        // ✅ ADD THIS
        string GenerateEmployeeToken(OrganizationEmployee employee);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ─────────────────────────────────────────
        // For User and Organization
        // ─────────────────────────────────────────
        public string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.Role,           user.Role.ToString()),
                new Claim(ClaimTypes.Name,           user.FullName),
                new Claim("UserId",                  user.UserId.ToString())
            };

            var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                                        Convert.ToDouble(
                                            _configuration["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ─────────────────────────────────────────
        // For Employee only
        // ─────────────────────────────────────────
        public string GenerateEmployeeToken(OrganizationEmployee employee)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new Claim(ClaimTypes.Email,          employee.Email),
                new Claim(ClaimTypes.Role,           "Employee"),
                new Claim(ClaimTypes.Name,           employee.FullName),
                new Claim("UserId",                  employee.EmployeeId.ToString()),
                new Claim("OrganizationId",          employee.OrganizationId.ToString()),
                new Claim("EmployeeCode",            employee.EmployeeCode ?? "")
            };

            var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                                        Convert.ToDouble(
                                            _configuration["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}