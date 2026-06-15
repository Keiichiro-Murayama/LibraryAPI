using System;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Domains.Models;
using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Security;
using LibraryApi.Applications.Usecases.Authenticate.Interfaces;
using LibraryApi.Presentations.Configs;

namespace LibraryApi.Applications.Tests.Usecase.Authenticate.Interactors;
/// <summary>
/// ユースケース:[ログインする]を実現するインターフェイス実装のテストドライバ
/// </summary>
[TestClass]
[TestCategory("Usecase/Authenticate/Interactors")]
public class AuthenticateUserUsecaseTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // テストターゲット
    private static IAuthenticateUserUsecase? _usecase;
    // UserのCRUD操作リポジトリ
    private IUserRepository? _repository;
    // パスワードのハッシュ化と検証サービス
    private IPasswordHashingService? _service;

    /// <summary>
    /// テストクラスの初期化
    /// </summary>
    /// <param name="_"></param>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        // MSTestテスト用ログ出力ハンドルを設定する
        _testContext = context;
        // アプリケーション管理を生成
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false).Build();
        // サービスプロバイダ(DIコンテナ)の生成
        _provider = ApplicationDependencyExtensions.BuildAppProvider(config);
    }

    /// <summary>
    /// テストクラスクリーンアップ
    /// </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        // 生成したサービスプロバイダ(DIコンテナ)を破棄する
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
        _usecase =
        _scope.ServiceProvider.GetRequiredService<IAuthenticateUserUsecase>();
        _repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _service = _scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();
    }

    /// <summary>
    /// テストメソッド実行後の後処理
    /// </summary> 
    [TestCleanup]
    public void TestCleanup()
    {
        // スコープドサービスを破棄する
        _scope!.Dispose();
    }

    [TestMethod("ユーザー名と正しいパスワードでUserが返される")]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenUsernameAndPasswordAreCorrect()
    {
        // ユーザーを生成する
        string name = Guid.NewGuid().ToString("n").Substring(0, 10);
        var password = _service!.Hash("P@ssw0rd123!");
        var user = new User(name, password);

            // ユーザーを登録する
            await _repository!.CreateAsync(user);
            // 認証処理をする
            var authed = await _usecase!.AuthenticateAsync(name, "P@ssw0rd123!");
            // nullでないことを検証する
            Assert.IsNotNull(authed);
            // ユーザーIdを検証する
            Assert.AreEqual(user.UserUuid, authed.UserUuid);
            // ユーザー名を検証する
            Assert.AreEqual(name, authed.Username);

        
    }

    [TestMethod("ユーザーが存在しない場合、AuthenticationExceptionがスローされる")]
    public async Task AuthenticateAsync_ShouldThrow_WhenUserNotFound()
    {
        // AuthenticationExceptionがスローされることを検証する
        Exception ex = await Assert.ThrowsExceptionAsync<AuthenticationException>(async () =>
        {
            await _usecase!.AuthenticateAsync("dnfasdohg", "test");
        });
        // メッセージを検証する
        Assert.AreEqual("ユーザーが存在しません。", ex.Message);
    }

    [TestMethod("パスワードが不一致の場合、AuthenticationExceptionがスローされる")]
    public async Task AuthenticateAsync_ShouldThrow_WhenPasswordMismatch()
    {
        var random = new Random();
        var randomName = string.Empty;
        for (var i = 0; i < 5; i++)
        {
            randomName += (char)('a' + random.Next(26));
        }

        var user = new User(randomName, _service!.Hash("CorrectP@ss!"));

            // ユーザーを登録する
            await _repository!.CreateAsync(user);
            // AuthenticationExceptionがスローされることを検証する
            Exception ex = await Assert.ThrowsExceptionAsync<AuthenticationException>(async () =>
            {
                await _usecase!.AuthenticateAsync(randomName, "adgfashf");
            });
            // メッセージを検証する
            Assert.AreEqual("パスワードが一致しません。", ex.Message);

    }
}