using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Applications.Usecases.Users.Interfaces;
using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Security;
using LibraryApi.Presentations.Configs;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;

namespace LibraryApi.Applications.Tests.Usecases.Users.Interactors;

[TestClass]
[TestCategory("Usecase/Users/Interactors")]
public class RegisterUserUsecaseTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // テストターゲット
    private IRegisterUserUsecase? _usecase;
    // UserのCRUD操作リポジトリ
    private IUserRepository? _repository;
    // パスワードのハッシュ化と検証サービス
    private IPasswordHashingService? _service;

    /// <summary>
    /// テストクラスの初期化
    /// </summary>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _testContext = context;
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false).Build();
        _provider = ApplicationDependencyExtensions.BuildAppProvider(config);
    }

    /// <summary>
    /// テストクラスのクリーンアップ
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
        _scope = _provider!.CreateScope();
        _usecase = _scope.ServiceProvider.GetRequiredService<IRegisterUserUsecase>();
        _repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        _service = _scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();
    }

    /// <summary>
    /// テストメソッド実行後の後処理
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        _scope!.Dispose();
    }

    //存在するユーザ名を検索すると、Exits Exceptionがスローされるか
    [TestMethod("存在するユーザ名を検索すると、Exits Exceptionがスローされるか")]
    public async Task ExistsByUsernameAsync_ShoudlThrow_ExitsException()
    {
        var name = "test";
        Exception ex = await Assert.ThrowsExceptionAsync<ExistsException>(async () =>
        {
            await _usecase!.ExistsByUsernameAsync(name);
        });
    }

    //存在しないユーザ名を検索するとExits Exceptionがスローされない
    [TestMethod("存在しないユーザ名を検索すると、Exits Exceptionがスローされない")]
    public async Task ExistsByUsernameAsync_ShoudlNotThrow_ExitsException()
    {
        try
        {
            var name = Guid.NewGuid().ToString();
            await _usecase!.ExistsByUsernameAsync(name);
        }
        catch (ExistsException)
        {
            Assert.Fail("ユーザが存在しないにも関わらず、例外がスローされました");
        }
    }

    //有効なユーザを登録できる
    [TestMethod("有効なユーザを登録できる")]
    public async Task RegisterUserAsync_ShouldRegister_WhenValidUser()
    {
        string name = Guid.NewGuid().ToString("n").Substring(0, 10);
        var user = new User(name, "CollectPass");
        try
        {
            //登録実行
            await _usecase!.RegisterUserAsync(user);
            //検証
            var result = await _repository!.SelectByUsernameAsync(user.Username);
            Assert.IsNotNull(result);
            Assert.AreEqual(user.Username, result.Username);
            Assert.AreNotEqual("CollectPass", result.Password);
            Assert.IsTrue(_service!.Verify(result.Password, "CollectPass"));

        }
        catch (InternalException)
        {
            Assert.Fail("有効なユーザが登録できませんでした");
        }
    }
}