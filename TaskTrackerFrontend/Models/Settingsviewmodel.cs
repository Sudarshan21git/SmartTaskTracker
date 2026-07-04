namespace TaskTrackerFrontend.Models
{
    public class SettingsViewModel
    {
        public UserProfileModel UserProfile { get; set; }
        public ChangePasswordModel ChangePassword { get; set; }

        public SettingsViewModel()
        {
            UserProfile = new UserProfileModel();
            ChangePassword = new ChangePasswordModel();
        }
    }
}