using System.ComponentModel.DataAnnotations;

namespace LibraryApi.Presentations.ViewModels;
/// <summary>
/// ユースケース:[新商品を登録する]を実現するViewModel
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "ユーザー名またはメールアドレスは必須です。")]
    public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "パスワードは必須です。")]
    public string Password { get; set; } = string.Empty;
}