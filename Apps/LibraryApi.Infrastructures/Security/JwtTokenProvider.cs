using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LibraryApi.Domains.Models;
using LibraryApi.Applications.Security;
namespace LibraryApi.Infrastructures.Security;

/// <summary>
/// HMAC-SHA256によるJWT アクセストークンの発行・検証を行うインターフェイスの実装
/// </summary>
public class JwtTokenProvider : IJwtTokenProvider
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _time;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="options">JWT認証に必要な設定値を保持するクラス</param>
    /// <param name="timeProvider">テスト用に注入可能なTimeProvider(省略可)</param>
    public JwtTokenProvider(IOptions<JwtSettings> options, TimeProvider? timeProvider = null)
    {
        _settings = options.Value;
        _time = timeProvider ?? TimeProvider.System;
        // シークレットキーが存在しない場合、InvalidOperationExceptionをスローする
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new InvalidOperationException("JwtSettings.SecretKey が未設定です。");
    }


    /// <summary>
    /// アクセストークンを発行し、ドメインオブジェクト :JWTTokenを返す
    /// </summary>
    /// <param name="user">
    ///     ユーザーのドメインオブジェクト（UserUuid/Username/Emailを利用）
    /// </param>
    /// <param name="extraClaims">追加のクレーム(任意)</param>
    /// <returns>JWT文字列("header.payload.signature")</returns>
    public string IssueAccessToken(User user, IEnumerable<Claim>? extraClaims = null)
    {
        // 現在のUTC時刻を取得する(トークンの発行日時として使用する)
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        // 有効期限を計算する(現在時刻 + 設定された有効期限の分数)
        var expiresUtc = nowUtc.AddMinutes(_settings.ExpiresInMinutes);
        // Payload に含めるクレーム(JWTの主体情報)を生成する
        var claims = new List<Claim>
        {
            // Subject(このトークンが紐づくユーザーId)
            new(JwtRegisteredClaimNames.Sub, user.UserUuid),
            // JWTID(一意な識別子でトークンの一意性や再利用防止に利用できる)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            // IssuedAt(トークン発行時刻でUNIX秒で表現)
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(nowUtc).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        // unique_name:ユーザー名(表示用)
        // 認証後に HttpContext.User.Identity.Nameとして参照される
        claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.Username));
        // 呼び出し側で追加したいクレームがある場合はまとめて追加する
        if (extraClaims is not null)
        {
            claims.AddRange(extraClaims);
        }
        // トークンに署名するための資格情報を生成する
        var creds = new SigningCredentials(
            BuildSecurityKey(_settings.SecretKey),// 生成した秘密鍵
            SecurityAlgorithms.HmacSha256);// 署名アルゴリズムにはHMAC-SHA256を指定
        // JWTトークン本体を生成する
        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,             // iss: 発行者
            audience: _settings.Audience,         // aud: 対象者
            claims: claims,                       // payloadに格納するクレーム情報（sub, jti, iat など）
            notBefore: nowUtc,                    // nbf:この日時より前は無効（有効開始時刻）
            expires: expiresUtc,                  // exp:有効期限（この日時を過ぎると無効）
            signingCredentials: creds             // 署名情報（秘密鍵とアルゴリズム）
        );
        // JwtSecurityTokenオブジェクトを文字列にシリアライズする
        // 形式は「Header / Payload / Signature」の3区切り(Base64Urlエンコード)で構成される
        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
        // JWTトークン返す
        return tokenString;
    }

    /// <summary>
    /// トークンを検証して正当なら認証済みユーザーを表すオブジェクト:ClaimsPrincipalを返す
    /// </summary>
    /// <param name="token">検証対象のJWTアクセストークン</param>
    /// <returns>認証済みユーザーを表すオブジェクト:ClaimsPrincipal</returns>
    /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">
    /// 署名不正・期限切れやIssuer/Audience不一致など、検証に失敗した場合にスローする
    /// </exception>
    public ClaimsPrincipal ValidateToken(string token)
    {
        // JWT の検証を行うハンドラを生成する
        var handler = new JwtSecurityTokenHandler();
        // トークン検証のためのルール(Issuer / Audience / 署名キー / 有効期限など)を構築する
        var parameters = BuildValidationParameters(validateLifetime: true);
        // トークンを検証する(署名の正当性、Issuer / Audience の一致、有効期限(exp)のチェック
        // 成功すれば ClaimsPrincipal(認証済みユーザー情報)を返す
        return handler.ValidateToken(token, parameters, out _);
    }

    /// <summary>
    /// 有効期限切れのトークンからクレームを取り出す
    /// </summary>
    /// <param name="token">>期限切れのJWTアクセストークン</param>
    /// <returns>認証済みユーザーを表すオブジェクト:ClaimsPrincipal</returns>
    /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">
    /// 署名不正・期限切れやIssuer/Audience不一致など、検証に失敗した場合にスローする
    /// </exception>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        // JWTの検証を行うハンドラを生成する
        var handler = new JwtSecurityTokenHandler();
        
        // 有効期限(exp)は検証せず、署名やIssuer/Audience のみ検証するルールを構築する
        // 期限切れトークンからでもクレームを取り出せるようにする
        var parameters = BuildValidationParameters(validateLifetime: false);

        // トークンを検証し、ClaimsPrincipalを返す
        // 期限切れでもクレーム情報は取得可能
        // 署名不正やIssuer/Audience不一致の場合は例外をスローする
        return handler.ValidateToken(token, parameters, out _);
    }

    /// <summary>
    /// トークン検証ルール（TokenValidationParameters）を構築する
    /// </summary>
    /// <param name="validateLifetime">
    /// true:有効期限 (exp) を検証する。<br/>
    /// false:有効期限を無視し、署名やIssuer/Audienceのみ検証する
    /// </param>
    /// <returns></returns>
    private TokenValidationParameters BuildValidationParameters(bool validateLifetime) =>
        new()
        {
            // Issuer(iss)を検証するかどうか
            // 設定値が空でなければ検証を有効化
            ValidateIssuer = !string.IsNullOrEmpty(_settings.Issuer),
            ValidIssuer = _settings.Issuer,

            // Audience(aud)を検証するかどうか
            // 設定値が空でなければ検証を有効化
            ValidateAudience = !string.IsNullOrEmpty(_settings.Audience),
            ValidAudience = _settings.Audience,

            // 署名鍵を必ず検証する(必須)
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = BuildSecurityKey(_settings.SecretKey),

            // 有効期限(exp)の検証を行うかどうか
            // 通常はtrue、期限切れトークンからクレームを取り出す場合はfalse
            ValidateLifetime = validateLifetime,

        };

    /// <summary>
    /// SecretKey文字列から署名用の対称鍵(SymmetricSecurityKey)を生成する
    /// Base64形式で渡された場合、Base64 としてデコードして利用
    /// 通常の文字列の場合、UTF8 バイト列に変換して利用
    /// どちらの場合も最低32バイト以上（256bit）を推奨
    /// </summary>
    private static SecurityKey BuildSecurityKey(string secret)
    {
        try
        {
            //Base64のバイト配列に変換する
            var keyBytes = Convert.FromBase64String(secret);
            // 鍵の長さが32バイト未満ならHMAC-SHA256の安全性を満たさないので例外をスローする
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("SecretKey(Base64) が短すぎます。32バイト以上を推奨。");
            // 対称鍵オブジェクトを返す
            return new SymmetricSecurityKey(keyBytes);
        }
        catch (FormatException)
        {
            // Base64のバイト配列に変換できない場合、プレーン文字列としてUTF8バイト列に変換する
            var raw = Encoding.UTF8.GetBytes(secret);
            // 32バイト未満なら例外をスローする
            if (raw.Length < 32)
                throw new InvalidOperationException("SecretKey が短すぎます。32バイト以上を推奨。");
            // 対称鍵オブジェクトを返す
            return new SymmetricSecurityKey(raw);
        }
    }
}