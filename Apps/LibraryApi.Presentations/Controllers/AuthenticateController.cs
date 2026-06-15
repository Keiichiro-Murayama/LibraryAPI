using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryApi.Applications.Security;
using LibraryApi.Applications.Usecases.Authenticate.Interfaces;
using LibraryApi.Presentations.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace LibraryApi.Presentations.Controllers;

/// <summary>
/// ユースケース:[ログイン/ログアウト]を実現するコントローラ
/// </summary>
[ApiController]
[Route("library/api/auth")]
[SwaggerTag("ユーザー認証（ログイン/ログアウト）処理")]
public class AuthenticateController : ControllerBase
{
    private readonly IAuthenticateUserUsecase _usecase;
    private readonly IJwtTokenProvider _provider;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="usecase">ユースケース:[ログインする]を実現するインターフェイス</param>
    /// <param name="provider">JWTの発行・検証インターフェイス</param>
    public AuthenticateController(
        IAuthenticateUserUsecase usecase, IJwtTokenProvider provider)
    {
        _usecase = usecase;
        _provider = provider;
    }

    /// <summary>
    /// ログイン認証し、成功したらJWTトークンを返す
    /// </summary>
    /// <param name="model">ログイン情報ViewModel</param>
    /// <returns>認証成功時はJWTトークン、失敗時は401</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "ユーザーのログイン認証",
        Description = "ユーザー名またはメールアドレスとパスワードでログインを行い、JWTトークンを発行します。")]
    [SwaggerResponse(StatusCodes.Status200OK, "認証成功（JWTトークン返却）", typeof(TokenResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "認証失敗（ユーザーが存在しない、またはパスワード不一致）")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "バリデーションエラー")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        try
        {
            // 認証ユーザーを取得する
            var user = await _usecase.AuthenticateAsync(model.Username, model.Password);
            // JWTトークンを発行する
            var token = _provider.IssueAccessToken(user);

            // Cookieオプションを生成する
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // HttpOnlyを有効にする
                Secure = true, // HTTPS通信でのみ送信する
                SameSite = SameSiteMode.None, // クロスサイト送信を許可する
                Path = "/", // ルート配下すべてに適用
                            // Cookieの有効期限を設定60分にする
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            };
            // CookieにJWTトークンを追加する
            Response.Cookies.Append("AccessToken", token, cookieOptions);
            return Ok(new TokenResponse { Token = token });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "ユーザーのログアウト",
        Description = "JWTトークンを含むCookieを削除します。")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "ログアウト成功")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "認証失敗")]
    public IActionResult Logout()
    {
        // Cookie削除時も、発行時と同じオプションを指定する必要がある
        var deleteOptions = new CookieOptions
        {
            HttpOnly = true,  // JSから参照不可(発行時と同じ)
            Secure = true,  // HTTPS通信のみ(発行時と同じ)
            SameSite = SameSiteMode.None, // クロスサイト送信を許可(発行時と同じ)
            Path = "/", // ルート配下すべてに適用(発行時と同じ)
            Expires = DateTimeOffset.UnixEpoch  // 期限を過去日時に設定して無効化
        };
        // CookieからJWTトークンを削除する
        Response.Cookies.Delete("AccessToken", deleteOptions);
        return NoContent();
    }
}