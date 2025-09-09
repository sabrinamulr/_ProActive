using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Anja
{
    public partial class ChangePasswordInput
    {
        [Required, DataType(DataType.Password), Display(Name = "Aktuelles Passwort")]
        public string CurrentPasswordHash { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Neues Passwort")]
        [MinLength(8, ErrorMessage = "Mindestens 8 Zeichen.")]
        public string NewPasswordHash { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Neues Passwort bestätigen")]
        [Compare("NewPasswordHash", ErrorMessage = "Passwörter stimmen nicht überein.")]
        public string ConfirmNewPasswordHash { get; set; } = string.Empty;
    }
}
