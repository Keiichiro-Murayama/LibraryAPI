using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Domains.Models;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using LibraryApi.Presentations.Adapters;
using LibraryApi.Presentations.Configs;
using LibraryApi.Presentations.Controllers;
using LibraryApi.Presentations.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
namespace LibraryApi.Presentations.Tests.Controllers;
/// <summary>
/// ユースケース:[商品を変更する]を実現するコントローラのテストドライバ
/// </summary>
[TestClass]
[TestCategory("Controllers")]
public class SearchBookByKeywordControllerTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // ユースケース:[新商品を登録する]を実現するインターフェイス
    private ISearchBookByKeywordUsecase? _usecase;

    // テストターゲット
    private SearchBookByKeywordController? _controller;

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
        _usecase = _scope.ServiceProvider.GetRequiredService<ISearchBookByKeywordUsecase>();
        // テストターゲットを生成する
        _controller = new SearchBookByKeywordController(_usecase);
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

    [TestMethod("keywordに一致する図書の取得:存在する商品のkeywordで正常に取得（200OK）")]
    public async Task SearchBookByKeyword_ShouldReturnOK()
    {
        var response = await _controller!
            .Search("リーダブル");
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));

        var okObj = response as OkObjectResult;
        var searchResult = okObj!.Value as List<Book>;

        Assert.AreEqual(1, searchResult!.Count);
        // 図書を検証す
        Assert.AreEqual("94399b5c-7223-48c1-aab3-ea62378bdc13", searchResult![0].BookUuid);
        Assert.AreEqual("リーダブルコード", searchResult![0].Title);
    }

    [TestMethod("keywordに一致する図書が存在しない：Nullを返す（200OK)")]
    public async Task SearchBookByKeyword_ShouldReturnOKWithNull()
    {
        var response = await _controller!
            .Search("AAA");
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));

        var okObj = response as OkObjectResult;
        var bookList = okObj!.Value as List<Book>;
        Assert.IsFalse(bookList!.Any());
    }


    [TestMethod("keywordがnull：BadRequestを返す(400 BadRequest)")]
    public async Task SearchBookByKeyword_ShouldReturnBadRequest()
    {
        var response = await _controller!
            .Search(null);
        // レスポンスをBadRequestObjectResultに変換する
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));

        var bad = response as BadRequestObjectResult;
        // nullでないことを検証する
        Assert.IsNotNull(bad);
        // レスポンスボディを取得する
        var val = bad!.Value!;
        var code = val.GetType().GetProperty("code")?.GetValue(val) as string;
        var msg = val.GetType().GetProperty("message")?.GetValue(val) as string;
        Assert.AreEqual("INVALID_KEYWORD", code);
        Assert.AreEqual("検索キーワードを入力してください。", msg);
    }

}