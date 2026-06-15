using System.Security.Claims;
using LibraryApi.Domains.Models;

namespace LibraryApi.Applications.Security;
/// <summary>
/// JWTの発行・検証インターフェイス
/// </summary>
public interface IJwtTokenProvider
{
    /// <summary>
    /// アクセストークンを発行し、ドメインオブジェクト :JWTTokenを返す
    /// </summary>
    /// <param name="user">
    ///     ユーザーのドメインオブジェクト（UserUuid/Username/Emailを利用）
    /// </param>
    /// <param name="extraClaims">追加のクレーム(任意)</param>
    /// <returns>JWT文字列("header.payload.signature")</returns>
    string IssueAccessToken(User user, IEnumerable<Claim>? extraClaims = null);

    /// <summary>
    /// トークンを検証して正当なら認証済みユーザーを表すオブジェクト:ClaimsPrincipalを返す
    /// </summary>
    /// <param name="token">検証対象のJWTアクセストークン</param>
    /// <returns>認証済みユーザーを表すオブジェクト:ClaimsPrincipal</returns>
    /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">
    /// 署名不正・期限切れやIssuer/Audience不一致など、検証に失敗した場合にスローする
    /// </exception>
    ClaimsPrincipal ValidateToken(string token);

    /// <summary>
    /// 有効期限切れのトークンからクレームを取り出す
    /// </summary>
    /// <param name="token">>期限切れのJWTアクセストークン</param>
    /// <returns>認証済みユーザーを表すオブジェクト:ClaimsPrincipal</returns>
    /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">
    /// 署名不正・期限切れやIssuer/Audience不一致など、検証に失敗した場合にスローする
    /// </exception>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}