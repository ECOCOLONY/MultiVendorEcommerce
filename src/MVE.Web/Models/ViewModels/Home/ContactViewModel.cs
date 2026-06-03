using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels.Home;

public class ContactViewModel
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}
