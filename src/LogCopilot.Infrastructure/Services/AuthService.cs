using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LogCopilot.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly LogCopilotDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(LogCopilotDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = dto.OrganizationName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        organization.CreatedBy = user.Id;
        organization.UpdatedBy = user.Id;

        _context.Organizations.Add(organization);
        _context.Users.Add(user);

        var membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            Role = Role.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = user.Id,
            UpdatedBy = user.Id
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(user.Id, organization.Id, Role.Admin);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = "Admin"
            },
            Organization = new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name
            }
        };
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var membership = await _context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (membership == null)
            throw new InvalidOperationException("User has no organization membership");

        var accessToken = GenerateAccessToken(user.Id, membership.OrganizationId, membership.Role);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);

        return new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = membership.Role.ToString()
            },
            Organization = new OrganizationDto
            {
                Id = membership.Organization.Id,
                Name = membership.Organization.Name
            }
        };
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        if (token == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var membership = await _context.OrganizationMembers
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == token.UserId);

        if (membership == null)
            throw new InvalidOperationException("User has no organization membership");

        var accessToken = GenerateAccessToken(token.User.Id, membership.OrganizationId, membership.Role);
        var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(token.UserId);

        token.IsRevoked = true;
        await _context.SaveChangesAsync();

        return new AuthResultDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = new UserDto
            {
                Id = token.User.Id,
                Email = token.User.Email,
                FirstName = token.User.FirstName,
                LastName = token.User.LastName,
                Role = membership.Role.ToString()
            },
            Organization = new OrganizationDto
            {
                Id = membership.Organization.Id,
                Name = membership.Organization.Name
            }
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateAccessToken(Guid userId, Guid organizationId, Role role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("OrganizationId", organizationId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(Guid userId)
    {
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var tokenString = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return tokenString;
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
