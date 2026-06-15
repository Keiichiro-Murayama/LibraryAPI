namespace LibraryApi.Presentations.ViewModels;

/// <summary>
/// JWTトークンをクライアントに返すためのレスポンスモデル
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// JWTトークン文字列
    /// </summary>
    public string Token { get; set; } = string.Empty;
}