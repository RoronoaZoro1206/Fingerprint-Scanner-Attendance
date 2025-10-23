using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services;

public class SuperAdminInitializer
{
    private readonly IUserRepository _userRepository;
    private readonly IAdministrationRepository _administrationRepository;
    private readonly ILogger<SuperAdminInitializer> _logger;

    // BCrypt work factor constant (recommended: 12 for 2024)
    private const int BCRYPT_WORK_FACTOR = 12;

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

            // Optimized: Check if any admin users exist without loading all users
            var adminExists = await _userRepository.AdminExistsAsync();

            if (adminExists)
            {
                _logger.LogInformation("Admin user(s) already exist. Skipping super admin creation.");
                return;
            }

            _logger.LogInformation("No admin users found. Creating super admin account...");

            // Read super admin credentials from environment variables
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@fingerprint.com";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123";
            var adminFirstName = Environment.GetEnvironmentVariable("ADMIN_FIRST_NAME") ?? "Super";
            var adminLastName = Environment.GetEnvironmentVariable("ADMIN_LAST_NAME") ?? "Admin";
            var adminCode = Environment.GetEnvironmentVariable("ADMIN_CODE") ?? "ADMIN001";

            // Optimized: Check if email already exists with role check
            var existingUser = await _userRepository.GetByEmailWithRoleCheckAsync(adminEmail, 1);
            if (existingUser != null)
            {
                _logger.LogWarning($"Admin user with email {adminEmail} already exists. Skipping super admin creation.");
                return;
            }

            // Hash password using BCrypt.Net-Next 4.0.3 best practices
            // Work factor 12 = 2^12 = 4096 iterations (recommended for 2024)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, BCRYPT_WORK_FACTOR);

            _logger.LogInformation("Password hashed with BCrypt work factor: {WorkFactor}", BCRYPT_WORK_FACTOR);

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
                UserUpdatedAt = DateTime.Now,
                UserProfileImage = "default_profile.png"
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
                _logger.LogInformation("???????????????????????????????????????????????????????");
                _logger.LogInformation("   Super Admin Account Created Successfully!");
                _logger.LogInformation("???????????????????????????????????????????????????????");
                _logger.LogInformation("   Email:       {Email}", adminEmail);
                _logger.LogInformation("   Password:    {Password}", adminPassword);
                _logger.LogInformation("   Admin Code:  {AdminCode}", adminCode);
                _logger.LogInformation("   BCrypt Work: {WorkFactor} (4096 iterations)", BCRYPT_WORK_FACTOR);
                _logger.LogInformation("???????????????????????????????????????????????????????");
                _logger.LogWarning("??  IMPORTANT: Change the password after first login!");
                _logger.LogInformation("???????????????????????????????????????????????????????");
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
