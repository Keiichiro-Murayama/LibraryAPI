using System.Reflection;
using Microsoft.OpenApi.Models;

namespace LibraryApi.Presentations.Configs;
/// <summary>
/// Swagger(Open API)のサービス登録を行う拡張クラス
/// - アノテーション有効化
/// - XML コメントの取り込み
/// - JWT(Bearer)のセキュリティ定義と要件の追加
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Swaggerを有効化し、JWT認証をSwaggerUIから利用できるように設定する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="xmlDocAssembly">
    /// XMLドキュメントコメントを取り込む対象のアセンブリ
    /// </param>
    /// <returns>IServiceCollection(チェーン可能)</returns>
    public static IServiceCollection AddSwaggerWithJwt(
       this IServiceCollection services, Assembly? xmlDocAssembly = null)
    {
        // Swaggerを有効化する
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // ドキュメント定義（[Authorize]ボタンを表示）
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "📚 C#(ASP.NET Core) LibraryAPI",
                Version = "v1",
                Description = "このAPIは **JWT認証** を利用しています。\n\n" +
                    "1. [Authorize]ボタンをクリックして、トークンを入力して認証\n" +
                    "2. 認証が必要なエンドポイントを試ことができます\n" +
                    "3. 認証なしのアクセスが拒否されることも確認できます",
   
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
            // アノテーションを有効化(SwaggerTagやSwaggerResponseを反映）
            c.EnableAnnotations();
            // XMLコメントをSwaggerに取り込む（<summary>などを反映）
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            // JWT(Bearer)認証のセキュリティスキームを定義する
            // SwaggerUIの[Authorize]ボタンからトークンを入力できるようにする
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT認証トークンをそのままの形式で入力してください<br/>" +
                              "例）eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                Name = "Authorization",             // 送信先ヘッダー名
                In = ParameterLocation.Header,      // HTTP ヘッダに載せる
                Type = SecuritySchemeType.Http,     // HTTP 認証スキーム
                Scheme = "bearer",                  // "Bearer"ではなく小文字"bearer"が仕様
                BearerFormat = "JWT"
            });

            // 既定で全エンドポイントにBearerを要求する
            // SwaggerUIで一度認証すれば、すべてのAPI呼び出しにAuthorizationヘッダが付与される
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new List<string>() // OAuth2向け:Bearerでは通常空
                }
            });

        });
        return services;
    }
}