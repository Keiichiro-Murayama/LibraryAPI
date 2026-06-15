using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using Microsoft.Extensions.Configuration;
using LibraryApi.Presentations.Configs;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;
using LibraryApi.Presentations.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Presentations.Tests.Controllers;

[TestClass]
[TestCategory("Controllers")]
public class GetBookInfoByBookIdControllerTest
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // ユースケース:[新商品を登録する]を実現するインターフェイス
    private IGetBookInfoByBookIdUsecase? _usecase;

    // テストターゲット
    private GetBookInfoByBookIdController? _controller;

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
    /// テストメソッド実行の前処理
    /// </summary>
    [TestInitialize]
    public void TestInit()
    {
        // スコープドサービスを取得する
        _scope = _provider!.CreateScope();
        // [新商品を登録する]を実現インターフェイスを取得する
        _usecase = _scope.ServiceProvider.GetRequiredService<IGetBookInfoByBookIdUsecase>();
        // テストターゲットを生成する
        _controller = new GetBookInfoByBookIdController(_usecase);
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


    [TestMethod("存在する図書Idで図書を検索する：（200OK）")]
    public async Task GetInfoAsync_ShouldReturnOk()
    {
        // Act: コントローラーのメソッドを呼び出す
        var response = await _controller!
            .GetInfo("94399b5c-7223-48c1-aab3-ea62378bdc13");

        // Assert: 1. 返却された型が OkObjectResult (200 OK) であることを検証する
        var okResult = response as Microsoft.AspNetCore.Mvc.OkObjectResult;
        Assert.IsNotNull(okResult, "レスポンスが OkObjectResult ではありません。");
        Assert.AreEqual(200, okResult.StatusCode);

        // Assert: 2. 中身のデータ（型は実際の定義に合わせてBook等に変更してください）を取り出す
        // ※ここでは仮に型を Book としています。
        var result = okResult.Value as Book;
        Assert.IsNotNull(result, "OkObjectResult の中身が期待するデータ型ではありません。");

        // Assert: 3. 各プロパティの値を検証する（ユースケースのテストと同様）
        // 図書Idを検証する
        Assert.AreEqual("94399b5c-7223-48c1-aab3-ea62378bdc13", result.BookUuid);
        // 図書名を検証する（※コメントを「商品名」から「図書名」に変更しています）
        Assert.AreEqual("リーダブルコード", result.Title);
        // 著者を検証する
        Assert.AreEqual("Dustin Boswell", result.Author);

        // 在庫Idを検証する
        Assert.IsNotNull(result.Stock, "Stockがnullです。");
        Assert.AreEqual("d1a3c77a-b148-4162-8dde-e5229f26cd48", result.Stock.StockUuid);
        // 在庫数を検証する
        Assert.AreEqual(5, result.Stock.Stock);

        // 図書カテゴリIdを検証する
        Assert.IsNotNull(result.Category, "Categoryがnullです。");
        Assert.AreEqual("18836923-5194-47f1-bf4c-e09eb5fa8fef", result.Category.CategoryUuid);
        // 図書カテゴリ名を検証する
        Assert.AreEqual("技術書", result.Category.Name);
    }


    [TestMethod("存在しない図書を検索：404NotFound")]
    public async Task GetInfoAsync_ShouldReturn404NotFound()
    {
        // Act: 存在しないIDを指定してコントローラーを呼び出す
        var response = await _controller!
            .GetInfo("2f5016b6-6f6b-11f0-954a-00155d1bd30a");
        Assert.IsInstanceOfType(response, typeof(NotFoundObjectResult));

        
        var notFound = response as NotFoundObjectResult;
        Assert.IsNotNull(notFound);

        var val = notFound!.Value!;
        var code = val.GetType().GetProperty("code")?.GetValue(val) as string;
        var msg = val.GetType().GetProperty("message")?.GetValue(val) as string;
        Assert.AreEqual("BookNotFound", code);
        Assert.AreEqual("指定された図書が存在しません", msg);


    }
}


