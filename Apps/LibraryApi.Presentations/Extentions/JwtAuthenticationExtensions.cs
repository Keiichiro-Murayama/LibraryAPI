using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using LibraryApi.Applications.Security;
namespace LibraryApi.Presentations.Configs;
/// <summary>
/// JWT認証のサービス登録を行う拡張クラス
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// JwtBearerミドルウェアを登録し、TokenValidationParametersとイベントフックを設定する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="config">構成情報</param>
    /// <returns>IServiceCollection(チェーン可能)</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        // appsettings.jsonからJwtSettingsセクションを読み取る
        var section = config.GetSection("JwtSettings");
        // 読み取れない場合は例外をスローする
        var jwt = section.Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettingsが読み込めません。");
        // SecretKeyが未設定の場合は例外をスローする
        if (string.IsNullOrWhiteSpace(jwt.SecretKey))
            throw new InvalidOperationException("JwtSettings.SecretKeyが未設定です。");

        static SecurityKey BuildSecurityKey(string secret)
        {
            try
            {
                // secretをBase64文字列として解釈する
                var keyBytes = Convert.FromBase64String(secret);
                // Base64デコード後のバイト数を確認し、32バイト未満は危険なので例外にする
                if (keyBytes.Length < 32)
                    throw new InvalidOperationException("SecretKey(Base64) が短すぎます。32バイト以上を推奨。");
                // Base64形式が有効だった場合はそのまま対称鍵を返す
                return new SymmetricSecurityKey(keyBytes);
            }
            catch (FormatException)
            {
                // Base64形式として解釈できなかった場合
                // UTF8文字列としてバイト配列に変換する
                var raw = Encoding.UTF8.GetBytes(secret);

                // UTF8変換でも32バイト未満なら不十分として例外をスロー
                if (raw.Length < 32)
                    throw new InvalidOperationException("SecretKey が短すぎます。32バイト以上を推奨。");

                // UTF8バイト列から対称鍵を生成して返す
                return new SymmetricSecurityKey(raw);
            }
        }

        // JWTBearer認証をデフォルトスキームとして利用することを指定
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        // JWTBearer認証オプションを設定する
        .AddJwtBearer(options =>
        {
            // トークン検証ルールの設定
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrEmpty(jwt.Issuer), // Issuer(発行者)を検証
                ValidIssuer = jwt.Issuer,            // 有効なIssuer
                ValidateAudience = !string.IsNullOrEmpty(jwt.Audience),// Audience(利用者)を検証
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,     // 署名鍵を検証
                IssuerSigningKey = BuildSecurityKey(jwt.SecretKey), // 検証用の秘密鍵
                ValidateLifetime = true,             // 有効期限を検証
                ClockSkew = TimeSpan.Zero,// トークン有効期限チェック時の許容誤差
            };
        });
        return services;
    }
}