using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Anja;

public class LoginInput
{
    [Required(ErrorMessage = "Bitte Personalnummer eingeben.")]
    [Range(1, int.MaxValue, ErrorMessage = "Die Personalnummer muss größer 0 sein.")]
    public int Personalnummer { get; set; }

    [Required(ErrorMessage = "Bitte Passwort eingeben.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
