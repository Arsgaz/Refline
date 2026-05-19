using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Refline.Api.Contracts.InternalCompanies;
using Refline.Api.Data;
using Refline.Api.Entities;
using Refline.Api.Enums;
using Refline.Api.Security;

namespace Refline.Api.Services.InternalCompanies;

public sealed class CompanyProvisioningService(
    ReflineDbContext dbContext,
    ILogger<CompanyProvisioningService> logger)
{
    private const int BasicMaxDevices = 5;
    private static readonly DateTimeOffset PerpetualExpirationUtc = new(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);
    private static readonly char[] PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray();

    public async Task<CompanyProvisioningResult> ProvisionAsync(
        ProvisionCompanyRequestDto request,
        CancellationToken cancellationToken)
    {
        var companyName = request.CompanyName.Trim();
        var adminFullName = request.AdminFullName.Trim();
        var adminLogin = request.AdminLogin.Trim();

        if (string.IsNullOrWhiteSpace(companyName))
        {
            return CompanyProvisioningResult.Failure(
                CompanyProvisioningErrorType.Validation,
                "CompanyName is required.");
        }

        if (string.IsNullOrWhiteSpace(adminFullName))
        {
            return CompanyProvisioningResult.Failure(
                CompanyProvisioningErrorType.Validation,
                "AdminFullName is required.");
        }

        if (string.IsNullOrWhiteSpace(adminLogin))
        {
            return CompanyProvisioningResult.Failure(
                CompanyProvisioningErrorType.Validation,
                "AdminLogin is required.");
        }

        if (!Enum.IsDefined(request.LicenseType))
        {
            return CompanyProvisioningResult.Failure(
                CompanyProvisioningErrorType.Validation,
                "LicenseType is invalid.");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = PasswordHashHelper.ComputeHash(temporaryPassword);
        var issuedAt = DateTimeOffset.UtcNow;
        var licenseSettings = ComputeLicenseSettings(request.LicenseType, issuedAt);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var company = new Company
        {
            Name = companyName,
            IsActive = true,
            CreatedAt = issuedAt
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        var loginExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Login == adminLogin,
                cancellationToken);

        if (loginExists)
        {
            return CompanyProvisioningResult.Failure(
                CompanyProvisioningErrorType.Conflict,
                $"Admin login '{adminLogin}' already exists.");
        }

        var adminUser = new User
        {
            CompanyId = company.Id,
            FullName = adminFullName,
            Login = adminLogin,
            PasswordHash = passwordHash,
            Role = UserRole.Admin,
            ManagerId = null,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = issuedAt
        };

        dbContext.Users.Add(adminUser);

        var license = new License
        {
            CompanyId = company.Id,
            LicenseKey = await GenerateUniqueLicenseKeyAsync(company.Id, request.LicenseType, cancellationToken),
            MaxDevices = licenseSettings.MaxDevices,
            LicenseType = request.LicenseType,
            IssuedAt = issuedAt,
            ExpiresAt = licenseSettings.ExpiresAt,
            IsActive = true
        };

        dbContext.Licenses.Add(license);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Provisioned company {CompanyId} with initial admin {AdminUserId} and {LicenseType} license {LicenseId}.",
            company.Id,
            adminUser.Id,
            license.LicenseType,
            license.Id);

        return CompanyProvisioningResult.Success(new ProvisionCompanyResponseDto
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            AdminUserId = adminUser.Id,
            AdminLogin = adminUser.Login,
            TemporaryPassword = temporaryPassword,
            LicenseId = license.Id,
            LicenseKey = license.LicenseKey,
            LicenseType = license.LicenseType
        });
    }

    private static (int MaxDevices, DateTimeOffset ExpiresAt) ComputeLicenseSettings(
        LicenseType licenseType,
        DateTimeOffset issuedAt)
    {
        return licenseType switch
        {
            LicenseType.Basic => (BasicMaxDevices, PerpetualExpirationUtc),
            LicenseType.Corporate => (int.MaxValue, issuedAt.AddMonths(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(licenseType), licenseType, "Unsupported license type.")
        };
    }

    private async Task<string> GenerateUniqueLicenseKeyAsync(
        long companyId,
        LicenseType licenseType,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            var randomSegment = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var typeSegment = licenseType.ToString().ToUpperInvariant();
            var licenseKey = $"REFLINE-{typeSegment}-{companyId:D6}-{timestamp}-{randomSegment}";

            var exists = await dbContext.Licenses
                .AsNoTracking()
                .AnyAsync(license => license.LicenseKey == licenseKey, cancellationToken);

            if (!exists)
            {
                return licenseKey;
            }
        }

        throw new InvalidOperationException("Failed to generate a unique license key.");
    }

    private static string GenerateTemporaryPassword()
    {
        Span<byte> randomBytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(randomBytes);

        Span<char> randomChars = stackalloc char[10];
        for (var i = 0; i < randomBytes.Length; i++)
        {
            randomChars[i] = PasswordAlphabet[randomBytes[i] % PasswordAlphabet.Length];
        }

        return $"Rf-{new string(randomChars[..5])}-{new string(randomChars[5..])}";
    }
}
