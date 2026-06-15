using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Domains.Models;
using LibraryApi.Domains.Repositories;
using LibraryApi.Infrastructures.Contexts;
using LibraryApi.Presentations.Configs;
namespace LibraryApi.Infrastructures.Tests.Repositories;
/// <summary>
/// ドメインオブジェクト:UserのCRUD操作インターフェイス実装の単体テストドライバ
/// </summary>
[TestClass]
[TestCategory("Repositories")]
public class UserRepositoryTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // App用DbContext
    private static AppDbContext? _dbContext;
    // テストターゲット
    private static IUserRepository _userRepository = null!;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;

    /// <summary>
    /// テストクラスの初期化
    /// </summary>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // MSTestテスト用ログ出力ハンドルを設定する
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
        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        // DbContextを取得する
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// テストの後処理
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        _scope!.Dispose();
    }

    [TestMethod("ユーザーを永続化できる")]
    public async Task CreateAsync_ShouldPersistUser()
    {
        // Arrange
        var user = new User("taro_user", "hashedpwd"); // UUIDは自動生成

        // MySQLのExecutionStrategy配下で手動Txを1単位として実行
        var strategy = _dbContext!.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext!.Database.BeginTransactionAsync();
            try
            {
                // Act
                await _userRepository.CreateAsync(user);

                // Assert: メールで取得して一致を検証
                var persisted = await _userRepository.SelectByUsernameAsync("taro_user");
                Assert.IsNotNull(persisted);
                Assert.AreEqual(user.UserUuid, persisted!.UserUuid);
                Assert.AreEqual("taro_user", persisted.Username);
                Assert.AreEqual("hashedpwd", persisted.Password);
            }
            finally
            {
                await tx.RollbackAsync();
                tx.Dispose();
                _testContext!.WriteLine("トランザクションをロールバックしました。");
            }
        });
    }


    [TestMethod("ユーザー名またはメールが存在するとtrueが返る")]
    public async Task ExistsByUsernameOrEmailAsync_WhenExists_ShouldReturnTrue()
    {
        var user = new User("hanako_user","pwdhash11");

        var strategy = _dbContext!.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext!.Database.BeginTransactionAsync();
            try
            {
                // 事前に作成
                await _userRepository.CreateAsync(user);
                // ユーザー名でヒット
                var byName = await _userRepository.ExistsByUsernameAsync("hanako_user");
                Assert.IsTrue(byName);

            }
            finally
            {
                await tx.RollbackAsync();
                tx.Dispose();
                _testContext!.WriteLine("トランザクションをロールバックしました。");
            }
        });
    }

    [TestMethod("ユーザー名またはメールが存在しないとfalseが返る")]
    public async Task ExistsByUsernameOrEmailAsync_WhenNotExists_ShouldReturnFalse()
    {
        var result = await _userRepository.ExistsByUsernameAsync("nobody");
        Assert.IsFalse(result);
    }

    [TestMethod("ユーザー名またはメールからユーザーを取得できる（ユーザー名）")]
    public async Task SelectByUsernameAsync_ByUsername_ShouldReturnUser()
    {
        string name = Guid.NewGuid().ToString("n").Substring(0, 10);
        var u = new User(name, "hash1234");

        var strategy = _dbContext!.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext!.Database.BeginTransactionAsync();
            try
            {
                await _userRepository.CreateAsync(u);

                var result = await _userRepository.SelectByUsernameAsync(name);
                Assert.IsNotNull(result);
                Assert.AreEqual(u.UserUuid, result!.UserUuid);
                Assert.AreEqual(name, result.Username);
                Assert.AreEqual("hash1234", result.Password);
            }
            finally
            {
                await tx.RollbackAsync();
                tx.Dispose();
                _testContext!.WriteLine("トランザクションをロールバックしました。");
            }
        });
    }

    [TestMethod("ユーザー名またはメールに一致しない場合はnullが返る")]
    public async Task SelectByUsernameOrEmailAsync_WhenNoMatch_ShouldReturnNull()
    {
        var result = await _userRepository
            .SelectByUsernameAsync("fafd");
        Assert.IsNull(result);
    }






    [TestMethod("ユーザーIdでユーザーを取得できる")]
    public async Task SelectByIdAsync_WhenExists_ShouldReturnUser()
    {
        string name = Guid.NewGuid().ToString("n").Substring(0, 10);
        var u = new User(name, "hash1234");

        var strategy = _dbContext!.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext!.Database.BeginTransactionAsync();
            try
            {
                await _userRepository.CreateAsync(u);

                var result = await _userRepository.SelectByIdAsync(u.UserUuid);
                Assert.IsNotNull(result);
                Assert.AreEqual(u.UserUuid, result!.UserUuid);
                Assert.AreEqual(name, result.Username);
                Assert.AreEqual("hash1234", result.Password);
            }
            finally
            {
                await tx.RollbackAsync();
                tx.Dispose();
                _testContext!.WriteLine("トランザクションをロールバックしました。");
            }
        });
    }
    
    [TestMethod("ユーザーIdに一致しない場合はnullが返る")]
    public async Task SelectByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _userRepository.SelectByIdAsync(Guid.NewGuid().ToString());
        Assert.IsNull(result);
    }
}