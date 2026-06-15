using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LibraryApi.Domains.Models;
using LibraryApi.Applications.Security;
using LibraryApi.Infrastructures.Security;
using LibraryApi.Presentations.Configs;

namespace LibraryApi.Infrastructures.Tests.Security;
/// <summary>
/// IJwtTokenProviderインタフェース実装のテストドライバ
/// </summary>
[TestClass]
[TestCategory("Security")]
public class JwtTokenProviderTests
{
    // MSTest テスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープ
    private IServiceScope? _scope;
    // テストターゲット
    private static IJwtTokenProvider _jwt = null!;

    // ---- 固定時刻を返す TimeProvider（期限切れテスト用） ----
    private sealed class FixedTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    /// <summary>テストクラス初期化</summary>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _testContext = context;
        // アプリケーション構成を読み込む
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        // サービスプロバイダ(DIコンテナ)の生成
        _provider = ApplicationDependencyExtensions.BuildAppProvider(config);
    }

    /// <summary>
    /// テストクラスクリーンアップ
    /// </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _provider?.Dispose();
    }

    /// <summary>
    /// テストの前処理
    /// </summary>
    [TestInitialize]
    public void TestInit()
    {
        // スコープドサービスを取得する
        _scope = _provider!.CreateScope();
        // テストターゲットを取得する
        _jwt = _scope.ServiceProvider.GetRequiredService<IJwtTokenProvider>();
    }

    /// <summary>
    /// テストの後処理
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        _scope!.Dispose();
    }

    [TestMethod("発行したトークンを検証できる(sub/unique_name/email が取得できる)")]
    public void Issue_Then_Validate_Succeeds_And_ClaimsReadable()
    {
        // データを用意する
        var user = new User("alice", "pass0011");
        // トークンを生成する
        var token = _jwt.IssueAccessToken(user);
        // nullでないことを検証する
        Assert.IsNotNull(token);
        // トークン文字列が空や空白でないことを検証する
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));

        // トークンを検証する
        var principal = _jwt.ValidateToken(token);
        // クレームを読み取る
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
            ?? principal.Identity?.Name;
       
        // Idを検証する
        Assert.AreEqual(user.UserUuid, sub);
        // ユーザー名を検証する
        Assert.AreEqual(user.Username, name);

    }

    [TestMethod("署名鍵が異なる場合、SecurityTokenExceptionをスローする")]
    public void Validate_Fails_When_SignatureKey_Is_Wrong()
    {
        //  発行者と利用者を用意する
        var issuer = "Exercise:Backend";
        var audience = "Exercise:Frontend";
        // トークンを発行する
        var user = new User("bob" , "pass0011");
        var goodToken = _jwt.IssueAccessToken(user);

        // 異なるSecretKeyのプロバイダを手動生成
        var badOptions = Options.Create(new JwtSettings
        {
            Issuer = issuer,
            Audience = audience,
            SecretKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray().AsSpan(0, 16).ToArray().Concat(new byte[16]).ToArray()),
            ExpiresInMinutes = 60
        });
        var badProvider = new JwtTokenProvider(badOptions);
        try
        {
            _ = badProvider.ValidateToken(goodToken);
            Assert.Fail("SecurityTokenException 系の例外が投げられるべきです。");
        }
        catch (SecurityTokenException)
        {
            Assert.IsTrue(true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"SecurityTokenException 系が期待されましたが、実際は {ex.GetType().Name} でした。");
        }
    }

    [TestMethod("利用者(Audience)が異なる場合、SecurityTokenExceptionをスローする")]
    public void Validate_Fails_When_Audience_Is_Wrong()
    {
        // トークンを生成する
        var user = new User("carol", "pass0011");
        var token = _jwt.IssueAccessToken(user);
        // Audienceを変更したJWT認証に必要な設定値を用意する
        var altered = Options.Create(new JwtSettings
        {
            Issuer = "Exercise:Backend",
            Audience = "Exercise:Frontend/wrong",
            SecretKey = "THIS_IS_A_DEMO_SECRET_KEY_32+_CHARS!",
            ExpiresInMinutes = 60
        });
        var validator = new JwtTokenProvider(altered);
        try
        {
            _ = validator.ValidateToken(token);
            Assert.Fail("SecurityTokenException 系の例外が投げられるべきです。");
        }
        catch (SecurityTokenException)
        {
            Assert.IsTrue(true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"SecurityTokenException 系が期待されましたが、実際は {ex.GetType().Name} でした。");
        }
    }

    [TestMethod("期限切れトークンからクレームを抽出できる")]
    public void GetPrincipalFromExpiredToken_Returns_Claims_When_Expired()
    {
        // Arrange: 固定時刻で短期トークンを発行
        var fixedNow = new FixedTimeProvider(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var shortOptions = Options.Create(new JwtSettings
        {
            Issuer = "Exercise:Backend",
            Audience = "Exercise:Frontend",
            SecretKey = "THIS_IS_A_DEMO_SECRET_KEY_32+_CHARS!",
            ExpiresInMinutes = 1 // 1分で失効
        });

        var provider = new JwtTokenProvider(shortOptions, fixedNow);
        var user = new User("dave", "pass0011");
        var token = provider.IssueAccessToken(user);
        // 既定のClockSkewは5分なので、6分以上進めて確実に期限切れ扱いにする
        fixedNow.Advance(TimeSpan.FromMinutes(6));
        // クレームを抽出する
        var principal = provider.GetPrincipalFromExpiredToken(token);
        // nullでないことを検証する
        Assert.IsNotNull(principal);
        // Id を検証する（sub → NameIdentifier へのマッピングに対応）
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.AreEqual(user.UserUuid, sub);
    } 
    
    [TestMethod("SecretKey 未設定の場合はコンストラクタで例外が発生する")]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Ctor_Throws_When_Secret_Missing()
    {
        var bad = Options.Create(new JwtSettings
        {
            Issuer = "https://ex",
            Audience = "https://ex",
            SecretKey = "", // 未設定
            ExpiresInMinutes = 5
        });
            _ = new JwtTokenProvider(bad);
    }
}