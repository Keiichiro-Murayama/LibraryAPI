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
            // 発行したトークンをレスポンスボディで返す
            return Ok(new TokenResponse { Token = token });
        }
        catch (AuthenticationException ex)
        {
            // 認証失敗
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// ログアウト(ステートレス: バックエンド側では何もせず204返却)
    /// </summary>
    /// <returns>常に204 No Content</returns>
    [Authorize]
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "ユーザーのログアウト",
        Description = "JWTはステートレスなため、バックエンド側で無効化処理は行いません。クライアント側でトークンを破棄してください。")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "ログアウト成功（処理なし）")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}