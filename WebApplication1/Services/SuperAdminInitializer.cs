using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;
using BCrypt.Net;

namespace Fingerprint_Attendance_System.Services;

public class SuperAdminInitializer
{
    private readonly IUserRepository _userRepository;
    private readonly IAdministrationRepository _administrationRepository;
    private readonly ILogger<SuperAdminInitializer> _logger;

    public SuperAdminInitializer(
        IUserRepository userRepository,
        IAdministrationRepository administrationRepository,
        ILogger<SuperAdminInitializer> logger)
    {
        _userRepository = userRepository;
        _administrationRepository = administrationRepository;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Checking for super admin account...");

            // Check if any admin users exist (RoleId = 1 for admin)
            var allUsers = await _userRepository.GetAllAsync();
            var adminUsers = allUsers.Where(u => u.RoleId == 1).ToList();

            if (adminUsers.Any())
            {
                _logger.LogInformation($"Found {adminUsers.Count} admin user(s). Skipping super admin creation.");
                return;
            }

            _logger.LogInformation("No admin users found. Creating super admin account...");

            // Read super admin credentials from environment variables
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@fingerprint.com";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";
            var adminFirstName = Environment.GetEnvironmentVariable("ADMIN_FIRST_NAME") ?? "Super";
            var adminLastName = Environment.GetEnvironmentVariable("ADMIN_LAST_NAME") ?? "Admin";
            var adminCode = Environment.GetEnvironmentVariable("ADMIN_CODE") ?? "ADMIN001";

            // Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(adminEmail);
            if (existingUser != null)
            {
                _logger.LogWarning($"User with email {adminEmail} already exists. Skipping super admin creation.");
                return;
            }

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);

            // Create the user account
            var user = new User
            {
                UserEmail = adminEmail,
                UserPassword = hashedPassword,
                UserFirstName = adminFirstName,
                UserLastName = adminLastName,
                RoleId = 1, // Admin role
                IsActive = true,
                UserCreatedAt = DateTime.Now,
                UserUpdatedAt = DateTime.Now
            };

            var userCreated = await _userRepository.AddAsync(user);

            if (!userCreated)
            {
                _logger.LogError("Failed to create super admin user account.");
                return;
            }

            _logger.LogInformation("Super admin user account created successfully.");

            // Get the created user to get the UserId
            var createdUser = await _userRepository.GetByEmailAsync(adminEmail);
            if (createdUser == null)
            {
                _logger.LogError("Failed to retrieve created super admin user.");
                return;
            }

            // Create the administration record
            var administration = new Administration
            {
                UserId = createdUser.UserId,
                AdminCode = adminCode,
                AdminCreatedAt = DateTime.Now
            };

            var adminCreated = await _administrationRepository.AddAsync(administration);

            if (adminCreated)
            {
                _logger.LogInformation($"Super admin account created successfully!");
                _logger.LogInformation($"Email: {adminEmail}");
                _logger.LogInformation($"Password: {adminPassword}");
                _logger.LogInformation($"Admin Code: {adminCode}");
                _logger.LogInformation("Please change the password after first login.");
            }
            else
            {
                _logger.LogError("Failed to create administration record for super admin.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing super admin account: {Message}", ex.Message);
        }
    }
}
