using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using Microsoft.Extensions.Configuration;
using LibraryApi.Presentations.Configs;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;

namespace LibraryApi.Application.Tests.Usecase.Books.Interactors;
/// <summary>
/// ユースケース:図書をキーワードで検索する
/// </summary>
[TestClass]
[TestCategory("Usecase/Books/Interactor")]
public class SearchBookByKeywordUsecaseTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // テストターゲット
    private static ISearchBookByKeywordUsecase? _usecase;
    // 商品リポジトリ
    private static IBookRepository? _productRepository;

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
        _scope.ServiceProvider.GetRequiredService<ISearchBookByKeywordUsecase>();
        // 商品リポジトリを取得する
        _productRepository =
        _scope.ServiceProvider.GetRequiredService<IBookRepository>();
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

    [TestMethod("存在する商品キーワードで商品を取得できる")]
    public async Task ExecuteAsync_ShouldReturnProducts_WhenKeywordExists()
    {
        var results = await _usecase!.ExecuteAsync("リーダブル");

        // nullでないことを検証する
        Assert.IsNotNull(results);
        // 件数が4件であること検証する
        Assert.AreEqual(1, results.Count);
        // 商品Idを検証する
        Assert.AreEqual("94399b5c-7223-48c1-aab3-ea62378bdc13", results[0].BookUuid);
        // 商品名を検証する
        Assert.AreEqual("リーダブルコード", results[0].Title);
        // 著者を検証する
        Assert.AreEqual("Dustin Boswell", results[0].Author);
        // 在庫数を検証する
        Assert.AreEqual(5, results[0].Stock!.Stock);
        // 商品カテゴリIdを検証する
        Assert.AreEqual(
        "18836923-5194-47f1-bf4c-e09eb5fa8fef", results[0].Category!.CategoryUuid);
        // 商品カテゴリ名を検証する
        Assert.AreEqual("技術書", results[0].Category!.Name);
    }

    [TestMethod("存在しない商品キーワードの場合、空のリストが返される")]
    public async Task ExecuteAsync_ShouldThrowNotFoundException_WhenKeywordDoesNotExist()
    {
        var result = await _usecase!.ExecuteAsync("adfadofh");
        // nullでないことを検証する
        Assert.IsNotNull(result);
        // 件数が0件であることを検証する
        Assert.AreEqual(0, result.Count);
    }
}