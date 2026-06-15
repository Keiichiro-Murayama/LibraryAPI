using LibraryApi.Domains.Repositories;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using Microsoft.Extensions.Configuration;
using LibraryApi.Presentations.Configs;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;


namespace LibraryApi.Applications.Tests.Usecases.Books.Interactors;

[TestClass]
[TestCategory("Usecase/Books/Interactor")]
public class UpdateBookUsecaseTests
{
    // MSTestテスト用ログ出力ハンドル
    private static TestContext? _testContext;
    // サービスプロバイダ(DIコンテナ)
    private static ServiceProvider? _provider;
    // スコープドサービス
    private IServiceScope? _scope;
    // テストターゲット
    private static IUpdateBookUsecase? _usecase;
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
        _scope.ServiceProvider.GetRequiredService<IUpdateBookUsecase>();
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

    [TestMethod("存在する商品IDを指定すると例外はスローされない")]
    public async Task ExistsByBookNameAsync_ShouldNotThrow_WhenNameExists()
    {
        // 存在する名前なので例外なく完了するはず
        //関数名がNameになっているが、本来はID
        await _usecase!.ExistsByBookNameAsync("94399b5c-7223-48c1-aab3-ea62378bdc13");
    }


    [TestMethod("存在しない商品名を指定するとExistsExceptionがスローされる")]
    public async Task ExistsByBookNameAsync_ShouldThrowExistsException_WhenNameDoesNotExist()
    {
        var ex = await Assert.ThrowsExceptionAsync<ExistsException>(async () =>
        {
            await _usecase!.ExistsByBookNameAsync("94399b5c-7223-48c1-aab3-ea62378bdc00");
        });
        Assert.AreEqual("図書名:94399b5c-7223-48c1-aab3-ea62378bdc00は存在しません。", ex.Message);
    }

    [TestMethod("存在する商品を更新できる")]
    public async Task UpdateAsync_ExistingBook_ShouldUpdateSuccessfully()
    {
        var uuid = "94399b5c-7223-48c1-aab3-ea62378bdc13";
        var category = new BookCategory("18836923-5194-47f1-bf4c-e09eb5fa8fef", "技術書");
        var stock = new BookStock(5);
        var book = new Book(uuid, "リーダブルコード", "Dustin Boswell", category, stock);

        await _usecase!.UpdateBookAsync(book);

        var updatedBook = await _productRepository!.SelectByNameWithIdAsync(book.Title);
        Assert.IsNotNull(updatedBook);
        Assert.AreEqual("リーダブルコード", updatedBook.Title);
        Assert.AreEqual("Dustin Boswell", updatedBook.Author);
        Assert.AreEqual(5, updatedBook.Stock!.Stock);
    }

    [TestMethod("存在しない商品を更新できず、NotFoundExceptionがスローされる")]
    public async Task UpdateAsync_NonExistingBook_ShouldThrowNotFoundException()
    {
        var nonExistingUuid = "94399b5c-7223-48c1-aab3-ea62378bdc00";
        var category = new BookCategory("18836923-5194-47f1-bf4c-e09eb5fa8fef", "技術書");
        var stock = new BookStock(5);
        var book = new Book(nonExistingUuid, "リーダブルコード", "Dustin Boswell", category, stock);

        var ex = await Assert.ThrowsExceptionAsync<NotFoundException>(async () =>
        {
            await _usecase!.UpdateBookAsync(book);
        });

        Assert.AreEqual($"図書Id:リーダブルコードの図書は存在しないため変更できません。", ex.Message);
    }
}
