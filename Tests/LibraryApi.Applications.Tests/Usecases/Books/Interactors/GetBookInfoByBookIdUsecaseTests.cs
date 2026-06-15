using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using Microsoft.Extensions.Configuration;
using LibraryApi.Presentations.Configs;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;

namespace LibraryApi.Application.Tests.Usecase.Books.Interactors;

[TestClass]
[TestCategory("Usecase/Books/Interactor")]
public class GetBookInfoByBookIdUsecaseTests 
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // テストターゲット
    private static IGetBookInfoByBookIdUsecase? _usecase;
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
        _scope.ServiceProvider.GetRequiredService<IGetBookInfoByBookIdUsecase>();
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

    [TestMethod("登録された図書Idで図書情報を取得できる。")]
    public async Task GetInfoAsync_ShouldReturnBooks_WhenIdExists()
    {
        var result = await _usecase!.GetInfoAsync("94399b5c-7223-48c1-aab3-ea62378bdc13");
        
        // nullでないことを検証する
        Assert.IsNotNull(result);
        // 商品Idを検証する
        Assert.AreEqual("94399b5c-7223-48c1-aab3-ea62378bdc13", result.BookUuid);
        // 商品名を検証する
        Assert.AreEqual("リーダブルコード", result.Title);
        // 著者を検証する
        Assert.AreEqual("Dustin Boswell", result.Author);
        //在庫Idを検証する
        Assert.AreEqual("d1a3c77a-b148-4162-8dde-e5229f26cd48",result.Stock!.StockUuid);
        // 在庫数を検証する
        Assert.AreEqual(5, result.Stock!.Stock);
        // 商品カテゴリIdを検証する
        Assert.AreEqual(
        "18836923-5194-47f1-bf4c-e09eb5fa8fef", result.Category!.CategoryUuid);
        // 商品カテゴリ名を検証する
        Assert.AreEqual("技術書", result.Category!.Name);
    }

    [TestMethod("存在しない図書を検索した場合、NotFoundExceptionをスローする")]
    public async Task GetInfoAsync()
    {
        var ex = await Assert.ThrowsExceptionAsync<NotFoundException>(async () =>
        {
            await _usecase!.GetInfoAsync("2f5016b6-6f6b-11f0-954a-00155d1bd30a");
        });
        Assert.AreEqual("指定された図書が存在しません", ex.Message);    }
}
