namespace LibraryApi.Presentations.Configs;
/// <summary>
/// アプリケーション用の同一オリジンポリシー(CORS設定)を作成する拡張クラス。
/// </summary>
public static class CorsServiceExtensions
{
    // CORSポリシーの一意な名前
    private const string CorsPolicyName = "AppCorsPolicy";
    // CORSポリシーの一意な名前を返す
    public static string GetPolicyName() => CorsPolicyName;

    /// <summary>
    /// CORSポリシーをサービスコレクションに登録する拡張メソッド
    /// appsettings.jsonの「Cors:AllowedOrigins」セクションを読み取り、
    /// 許可されたオリジンをもとにポリシーを構築する
    /// </summary>
    /// <param name="services">IServiceCollection(DIコンテナ)</param>
    /// <param name="configuration">アプリケーション構成情報</param>
    /// <returns>IServiceCollection(メソッドチェーン可能)</returns>
    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        // appsettings.json の "Cors:AllowedOrigins" セクションから
        // 許可オリジン(ドメイン/ポート/プロトコル)を配列として取得する
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        // AddCors(): CORS機能をサービスコレクションに登録する拡張メソッド
        // 引数には Action<CorsOptions> デリゲートを指定し、
        // AddPolicy() で実際のポリシー内容を定義する。
        services.AddCors(options =>
        {
            // AddPolicy():
            // 第1引数：ポリシー名
            // 第2引数：CorsPolicyBuilder型のpolicy引数を受け取り、設定を行うラムダ式
            options.AddPolicy(CorsPolicyName, policy =>
            {
                // policyはCorsPolicyBuilderクラスのインスタンス
                // ここで許可するオリジン・ヘッダ・メソッド・資格情報などを設定する
                policy
                    // appsettings.jsonで定義されたオリジンを許可する
                    .WithOrigins(allowedOrigins)
                    // 任意のHTTPヘッダを許可する(例: Content-Type, Authorizationなど）
                    .AllowAnyHeader()
                    // 任意のHTTPメソッド(GET, POST, PUT, DELETE...)を許可する
                    .AllowAnyMethod()
                    // 認証情報(Cookie, Tokenなど)の送信を許可する
                    .AllowCredentials();
            });
        });
        return services;
    }
}