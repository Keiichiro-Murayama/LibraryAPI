using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApi.Applications.Security;

public class JwtSettings
{
    /// <summary>
    /// トークン発行者(iss)
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// トークン利用者(aud)
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// 署名用のシークレットキー  
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// 有効期限(分単位)
    /// </summary>
    public int ExpiresInMinutes { get; init; } = 60;
}
