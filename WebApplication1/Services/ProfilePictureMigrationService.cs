using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Services;

/// <summary>
/// Service to migrate existing shared profile pictures to user-specific images
/// Run this ONCE when upgrading from shared images to one-picture-per-user model
/// </summary>
public class ProfilePictureMigrationService
{
    private readonly ILogger<ProfilePictureMigrationService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IWebHostEnvironment _environment;
    
    private const string TEMPLATES_FOLDER = "templates";
    private const string USERS_FOLDER = "users";
    private const string DEFAULT_PROFILE = "default_profile.png";

    public ProfilePictureMigrationService(
        ILogger<ProfilePictureMigrationService> logger,
        IUserRepository userRepository,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _userRepository = userRepository;
        _environment = environment;
    }

    /// <summary>
    /// Migrate all users to new profile picture structure
    /// Creates user-specific copies for each user
    /// Moves shared images to templates folder
    /// </summary>
    public async Task<MigrationResult> MigrateProfilePictures()
    {
        var result = new MigrationResult();
        
        try
        {
            _logger.LogInformation("Starting profile picture migration...");

            // Step 1: Create folder structure
            await CreateFolderStructure();

            // Step 2: Get all users
            var users = (await _userRepository.GetAllAsync()).ToList();
            result.TotalUsers = users.Count;

            _logger.LogInformation($"Found {users.Count} users to migrate");

            // Step 3: Process each user
            foreach (var user in users)
            {
                try
                {
                    if (string.IsNullOrEmpty(user.UserProfileImage))
                    {
                        // Set default for users without profile image
                        user.UserProfileImage = $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";
                        await _userRepository.UpdateAsync(user);
                        result.UsersSetToDefault++;
                        continue;
                    }

                    // Skip if already in new format (contains folder prefix)
                    if (user.UserProfileImage.Contains("/"))
                    {
                        result.UsersAlreadyMigrated++;
                        continue;
                    }

                    // Skip default profile
                    if (user.UserProfileImage == DEFAULT_PROFILE)
                    {
                        user.UserProfileImage = $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";
                        await _userRepository.UpdateAsync(user);
                        result.UsersSetToDefault++;
                        continue;
                    }

                    // Migrate user's profile picture
                    var migrated = await MigrateUserProfilePicture(user);
                    
                    if (migrated)
                    {
                        result.UsersMigrated++;
                    }
                    else
                    {
                        result.UsersFailed++;
                        result.FailedUsers.Add($"{user.UserEmail} (ID: {user.UserId})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error migrating user {user.UserId}");
                    result.UsersFailed++;
                    result.FailedUsers.Add($"{user.UserEmail} (ID: {user.UserId}) - {ex.Message}");
                }
            }

            // Step 4: Move remaining shared images to templates
            await MoveSharedImagesToTemplates();

            result.Success = true;
            result.Message = "Migration completed successfully";
            
            _logger.LogInformation($"Migration complete. Migrated: {result.UsersMigrated}, Already migrated: {result.UsersAlreadyMigrated}, Set to default: {result.UsersSetToDefault}, Failed: {result.UsersFailed}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
        }

        return result;
    }

    private async Task CreateFolderStructure()
    {
        var profilePath = Path.Combine(_environment.WebRootPath, "images", "profile");
        var templatesPath = Path.Combine(profilePath, TEMPLATES_FOLDER);
        var usersPath = Path.Combine(profilePath, USERS_FOLDER);

        if (!Directory.Exists(templatesPath))
        {
            Directory.CreateDirectory(templatesPath);
            _logger.LogInformation("Created templates folder");
        }

        if (!Directory.Exists(usersPath))
        {
            Directory.CreateDirectory(usersPath);
            _logger.LogInformation("Created users folder");
        }

        // Ensure default_profile.png is in templates
        var rootDefaultPath = Path.Combine(profilePath, DEFAULT_PROFILE);
        var templateDefaultPath = Path.Combine(templatesPath, DEFAULT_PROFILE);

        if (File.Exists(rootDefaultPath) && !File.Exists(templateDefaultPath))
        {
            File.Copy(rootDefaultPath, templateDefaultPath);
            _logger.LogInformation("Copied default_profile.png to templates folder");
        }
    }

    private async Task<bool> MigrateUserProfilePicture(Models.User user)
    {
        try
        {
            var profilePath = Path.Combine(_environment.WebRootPath, "images", "profile");
            var currentImagePath = Path.Combine(profilePath, user.UserProfileImage!);

            if (!File.Exists(currentImagePath))
            {
                _logger.LogWarning($"Image not found for user {user.UserId}: {user.UserProfileImage}");
                // Set to default
                user.UserProfileImage = $"{TEMPLATES_FOLDER}/{DEFAULT_PROFILE}";
                await _userRepository.UpdateAsync(user);
                return true;
            }

            // Create user-specific copy
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var extension = Path.GetExtension(user.UserProfileImage);
            var newFileName = $"user_{user.UserId}_{timestamp}{extension}";
            
            var usersFolder = Path.Combine(profilePath, USERS_FOLDER);
            var newFilePath = Path.Combine(usersFolder, newFileName);

            // Copy image to new location with user-specific name
            File.Copy(currentImagePath, newFilePath);

            // Update database
            user.UserProfileImage = $"{USERS_FOLDER}/{newFileName}";
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation($"Migrated user {user.UserId}: {user.UserEmail} → {newFileName}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to migrate user {user.UserId}");
            return false;
        }
    }

    private async Task MoveSharedImagesToTemplates()
    {
        try
        {
            var profilePath = Path.Combine(_environment.WebRootPath, "images", "profile");
            var templatesPath = Path.Combine(profilePath, TEMPLATES_FOLDER);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var rootImages = Directory.GetFiles(profilePath)
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            foreach (var imagePath in rootImages)
            {
                var fileName = Path.GetFileName(imagePath);
                
                // Skip default (already copied)
                if (fileName == DEFAULT_PROFILE)
                    continue;

                // Skip if already user-specific (shouldn't be in root after migration)
                if (fileName!.StartsWith("user_"))
                    continue;

                var templatePath = Path.Combine(templatesPath, fileName);

                if (!File.Exists(templatePath))
                {
                    File.Copy(imagePath, templatePath);
                    _logger.LogInformation($"Moved shared image to templates: {fileName}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving shared images to templates");
        }
    }

    public class MigrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int UsersMigrated { get; set; }
        public int UsersAlreadyMigrated { get; set; }
        public int UsersSetToDefault { get; set; }
        public int UsersFailed { get; set; }
        public List<string> FailedUsers { get; set; } = new();

        public override string ToString()
        {
            return $@"
Migration Result:
- Total Users: {TotalUsers}
- Migrated: {UsersMigrated}
- Already Migrated: {UsersAlreadyMigrated}
- Set to Default: {UsersSetToDefault}
- Failed: {UsersFailed}
{(FailedUsers.Any() ? $"\nFailed Users:\n{string.Join("\n", FailedUsers)}" : "")}
";
        }
    }
}
