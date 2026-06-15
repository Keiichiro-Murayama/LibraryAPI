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
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LibraryApi.Presentations.Tests.Controllers;

[TestClass]
[TestCategory("Controllers")]
public class UpdateBookControllerTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // ユースケース:[新商品を登録する]を実現するインターフェイス
    private IUpdateBookUsecase? _usecase;

    private UpdateBookViewModelAdapter? _adapter;
    private IBookRepository? _repository;

    // テストターゲット
    private UpdateBookController? _controller;

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
        _usecase = _scope.ServiceProvider.GetRequiredService<IUpdateBookUsecase>();
        _adapter = _scope.ServiceProvider.GetRequiredService<UpdateBookViewModelAdapter>();
        _repository = _scope.ServiceProvider.GetRequiredService<IBookRepository>();
        // テストターゲットを生成する
        _controller = new UpdateBookController(_usecase, _adapter);
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

    [TestMethod("矛盾がない場合、OKとへこうされた図書が返される")]
    public async Task Updateed_ShouedReturnOk()
    {
        // ViewModelを用意する
        var Uuid = "94399b5c-7223-48c1-aab3-ea62378bdc13";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = "リーダブルコード",
            Author = "Dustin Boswell",
            Stock = 5
        };
        var response = await _controller!.Updated(viewModel, Uuid);
        var ok = response as OkObjectResult;
        // Status Code が 200 OK であることを検証
        var okResult = response as OkObjectResult;
        Assert.IsNotNull(okResult, "レスポンスが OkObjectResult ではありません。");

        // 返却されたViewModelの内容を検証
        var resultViewModel = okResult!.Value as UpdateBookResponseViewModel;
        Assert.IsNotNull(resultViewModel, "返却された値が UpdateBookResponseViewModel ではありません。");

        // 各プロパティがリクエスト通りに変更されているかを検証
        Assert.AreEqual(Uuid, resultViewModel!.BookId);
        Assert.AreEqual(viewModel.Title, resultViewModel.Title);
        Assert.AreEqual(viewModel.Author, resultViewModel.Author);
        Assert.AreEqual(viewModel.Stock, resultViewModel.Stock);
    }

    [TestMethod("存在しない商品の場合、NotFoundExceptionを返す")]
    public async Task Update_Return404NotFound( )
    {
                // ViewModelを用意する
        var Uuid = "94399b5c-1123-48c1-aab3-ea62378bdc13";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = "リーダブル",
            Author = "Dustin Doswell",
            Stock = 3
        };
        
        var response = await _controller!.Updated(viewModel, Uuid);
         var notfound = response as NotFoundObjectResult;
        // nullでないことを検証する
        Assert.IsNotNull(notfound);
        // レスポンスボディを取得する
        var val = notfound.Value!;
        var code = (string)val.GetType().GetProperty("code")!.GetValue(val)!;
        var msg = (string)val.GetType().GetProperty("message")!.GetValue(val)!;
        // コードを検証する
        Assert.AreEqual("PRODUCT_NOT_FOUND", code);
        // エラーメッセージを検証する
        Assert.AreEqual($"図書名:94399b5c-1123-48c1-aab3-ea62378bdc13は存在しません。", msg);
    }

[TestMethod("更新在庫がマイナスで400BadRequestを返す")]
    public async Task Update_NegativeStock_Return400BadRequest()
    {
        var uuid = "a5fd08bc-5f56-441e-a315-edb8a1e02053";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = "セーラームーン",
            Author = "武内直子",
            Stock = -1
        };

        var response = await _controller!.Updated(viewModel, uuid);
        var badRequest = response as BadRequestObjectResult;

        Assert.IsNotNull(badRequest);
        var val = badRequest.Value!;
        var code = (string)val.GetType().GetProperty("code")!.GetValue(val)!;
        
        Assert.AreEqual("DOMAIN_RULE_VIOLATION", code);
    }

    [TestMethod("タイトルが未入力でBadRequestを返す")]
    public async Task Update_EmptyTitle_Return400BadRequest()
    {
        var uuid = "94399b5c-7223-48c1-aab3-ea62378bdc13";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = "",
            Author = "武内直子",
            Stock = 5
        };

        var response = await _controller!.Updated(viewModel, uuid);
        var Request = response as BadRequestObjectResult;

        Assert.IsNotNull(Request);
        var val = Request.Value!;
        var code = (string)val.GetType().GetProperty("code")!.GetValue(val)!;

        Assert.AreEqual("DOMAIN_RULE_VIOLATION", code);
    }

    [TestMethod("タイトルが51文字でBadRequestを返す")]
    public async Task Update_TitleTooLong_Return400BadRequest()
    {
        var uuid = "94399b5c-7223-48c1-aab3-ea62378bdc13";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = new string('あ', 51),
            Author = "武内直子",
            Stock = 5
        };

        var response = await _controller!.Updated(viewModel, uuid);
        var badRequest = response as BadRequestObjectResult;

        Assert.IsNotNull(badRequest);
        var val = badRequest.Value!;
        var code = (string)val.GetType().GetProperty("code")!.GetValue(val)!;

        Assert.AreEqual("DOMAIN_RULE_VIOLATION", code);
    }

    [TestMethod("著者が31文字でBadRequestを返す")]
    public async Task Update_AuthorTooLong_Return400BadRequest()
    {
        var uuid = "94399b5c-7223-48c1-aab3-ea62378bdc13";
        var viewModel = new UpdateBookRequestViewModel
        {
            Title = "セーラームーン",
            Author = new string('A', 31),
            Stock = 5
        };

        var response = await _controller!.Updated(viewModel, uuid);
        var badRequest = response as BadRequestObjectResult;

        Assert.IsNotNull(badRequest);
        var val = badRequest.Value!;
        var code = (string)val.GetType().GetProperty("code")!.GetValue(val)!;

        Assert.AreEqual("DOMAIN_RULE_VIOLATION", code);
    }


    

}