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
using LibraryApi.Presentations.Adapters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using LibraryApi.Presentations.ViewModels;
using System.ComponentModel;
using Microsoft.OpenApi.Writers;

namespace LibraryApi.Presentations.Tests.Controllers;

[TestClass]
[TestCategory("Controllers")]
public class RegisterBookControllerTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // ユースケース:[新商品を登録する]を実現するインターフェイス
    private IRegisterBookUsecase? _usecase;

    private RegisterBookViewModelAdapter? _adapter;
    private IBookRepository? _repository;

    // テストターゲット
    private RegisterBookController? _controller;

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
        _usecase = _scope.ServiceProvider.GetRequiredService<IRegisterBookUsecase>();
        _adapter = _scope.ServiceProvider.GetRequiredService<RegisterBookViewModelAdapter>();
        _repository =  _scope.ServiceProvider.GetRequiredService<IBookRepository>();
        // テストターゲットを生成する
        _controller = new RegisterBookController(_usecase, _adapter);
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



// =========================================================================
    // 正常系テスト（既存の不整合を修正）
    // =========================================================================

    [TestMethod("カテゴリーリストが正しく取得できる200OK")]
    public async Task GetCategoriesAsync_ReturnOK()
    {
        // 既存のコードの修正：コントローラのメソッド名が GetCategories なので修正
        var result = await _controller!.GetCategoriesAsync(); 
        var okResult = result as OkObjectResult;
        
        Assert.IsNotNull(okResult);
        // 修正：okResult.Value ではなく StatusCode を比較する
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode); 
        
        var categories = okResult.Value as List<BookCategory>;
        Assert.IsNotNull(categories);
        Assert.AreEqual(6, categories.Count);
    }

    [TestMethod("正しく登録される")]
    public async Task RegisterBookAsync_ReturnCreated()
    {
        // 準備（実DB/モックの兼ね合い上、テスト実行のたびに重複しないタイトルを推奨）
        var uniqueTitle = "セーラームーン_" + Guid.NewGuid().ToString().Substring(0, 8);
        var data = new RegisterBookRequestViewModel
        {
            Title = uniqueTitle,
            Author = "武内直子",
            Stock = 5,
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        var response = await _controller!.Register(data);

        var createdResult = response as CreatedResult;
        Assert.IsNotNull(createdResult);
        // 修正：CreatedResultのステータスコードは 201 Created です
        Assert.AreEqual(StatusCodes.Status201Created, createdResult.StatusCode); 

        // 修正：コントローラの実装上、ValueにはBookオブジェクトではなく「book.BookUuid」のみが入っています
        var bookUuid = createdResult.Value as string; 
        Assert.IsNotNull(bookUuid);

        // 注意：_repositoryがDIコンテナから取得されていない場合、ここでNullReferenceExceptionになります。
        // 必要に応じて TestInit 内で _scope.ServiceProvider.GetRequiredService<IBookRepository>() を行ってください。
        if (_repository != null)
        {
            await _repository.DeleteByIdAsync(bookUuid);
        }
    }

    // =========================================================================
    // 異常系テスト
    // =========================================================================

    [TestMethod("商品が重複して登録される：409Conflict")]
    public async Task RegisterBookAsync_DuplicateTitle_Return409Conflict()
    {
        string title = Guid.NewGuid().ToString("n").Substring(0, 5);
        string author = Guid.NewGuid().ToString("n").Substring(0, 5);
        // 1回目の登録（確実に重複させるために、まずは普通に登録するか、
        // または Usecase の ExistsByBookNameAsync が ExistsException を投げる状態を作る）
        var data = new RegisterBookRequestViewModel
        {
            Title = title,
            Author = author,
            Stock = 5,
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        // 1回目の登録（既にデータがある前提なら不要ですが、確実に走らせるため）
        await _controller!.Register(data);

        // 2回目の登録（同じタイトル）
        var response = await _controller.Register(data);

        Console.WriteLine($"#####################  Response type: {response?.GetType().Name}##############");
        var conflictResult = response as ConflictObjectResult;
        Assert.IsNotNull(conflictResult);
        Assert.AreEqual(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    [TestMethod("存在しない図書カテゴリIdを受信した：400BadRequest")]
    public async Task RegisterBookAsync_CategoryNotFound_Return400BadRequest()
    {
        // 準備（存在しないカテゴリIDを設定）
        var data = new RegisterBookRequestViewModel
        {
            Title = "テスト図書（カテゴリなし）",
            Author = "テスト著者",
            Stock = 1,
            CategoryId = "00000000-0000-0000-0000-000000000000" 
        };

        var response = await _controller!.Register(data);

        var badRequestResult = response as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);


    }

    [TestMethod("タイトルが0文字：400BadRequest")]
    public async Task RegisterBookAsync_TitleEmpty_Return400BadRequest()
    {
        var data = new RegisterBookRequestViewModel
        {
            Title = "", // 0文字
            Author = "テスト著者",
            Stock = 1,
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        _controller!.ModelState.AddModelError("Title", "書名は1〜50文字で入力してください");

        var response = await _controller.Register(data);

        var badRequestResult = response as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [TestMethod("著者名が31文字以上：400BadRequest")]
    public async Task RegisterBookAsync_AuthorTooLong_Return400BadRequest()
    {
        var data = new RegisterBookRequestViewModel
        {
            Title = "正常なタイトル",
            Author = new string('B', 31), // 31文字
            Stock = 1,
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        _controller!.ModelState.AddModelError("Author", "著者名は30文字以内で入力してください");

        var response = await _controller.Register(data);

        var badRequestResult = response as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [TestMethod("著者名が0文字：400BadRequest")]
    public async Task RegisterBookAsync_AuthorEmpty_Return400BadRequest()
    {
        var data = new RegisterBookRequestViewModel
        {
            Title = "正常なタイトル",
            Author = "", // 0文字
            Stock = 1,
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        _controller!.ModelState.AddModelError("Author", "著者名は必須です");

        var response = await _controller.Register(data);

        var badRequestResult = response as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [TestMethod("蔵書数がマイナス：400BadRequest")]
    public async Task RegisterBookAsync_StockNegative_Return400BadRequest()
    {
        var data = new RegisterBookRequestViewModel
        {
            Title = "正常なタイトル",
            Author = "テスト著者",
            Stock = -1, // マイナス値
            CategoryId = "18836923-5194-47f1-bf4c-e09eb5fa8fef"
        };

        _controller!.ModelState.AddModelError("Stock", "蔵書数は0以上の数値を入力してください");

        var response = await _controller.Register(data);

        var badRequestResult = response as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }
}
