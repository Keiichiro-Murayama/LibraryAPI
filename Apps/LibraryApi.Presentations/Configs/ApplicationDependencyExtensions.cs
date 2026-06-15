using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LibraryApi.Infrastructures.Contexts;
using LibraryApi.Infrastructures.Adapters;
using LibraryApi.Infrastructures.Repositories;
using LibraryApi.Infrastructures.Shared;
using LibraryApi.Infrastructures.Security;
using LibraryApi.Domains.Repositories;
using LibraryApi.Domains.Models;
using LibraryApi.Applications.Usecases;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using LibraryApi.Applications.Usecases.Books.Interactors;
using LibraryApi.Applications.Usecases.Users.Interfaces;
using LibraryApi.Applications.Usecases.Users.Interactors;
using LibraryApi.Applications.Usecases.Authenticate.Interfaces;
using LibraryApi.Applications.Usecases.Authenticate.Interactors;
using LibraryApi.Applications.Security;
using LibraryApi.Presentations.Adapters;
namespace LibraryApi.Presentations.Configs;
/// <summary>
/// 依存関係(DI)の設定
/// インフラストラクチャ層、アプリケーション層、プレゼンテーション層
/// をまとめて追加する拡張クラス
/// </summary>
public static class ApplicationDependencyExtensions
{
    /// <summary>
    /// アプリ全体の依存関係を一括追加する拡張メソッド
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="config">構成情報</param>
    /// <returns>IServiceCollection(チェーン可能)</returns>
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        // インフラストラクチャ層の依存関係を追加
        services.AddInfrastructureDependencies(config);
        // アプリケーション層の依存関係を追加
        services.AddApplicationLayerDependencies(config);
        // プレゼンテーション層の依存関係を追加
        services.AddPresentationLayerDependencies(config);
        return services;
    }

    /// <summary>
    /// インフラストラクチャ層の依存関係を追加
    /// </summary>
    /// <param name="services">依存関係注入(DI)のサービスコレクション</param>
    /// <param name="config">アプリケーションの設定情報を管理</param>
    /// <returns></returns>
    private static IServiceCollection AddInfrastructureDependencies(
       this IServiceCollection services, IConfiguration config)
    {
        // PostgreSQLの接続文字列を設定ファイルから取得する
        var connectstr = config.GetConnectionString("PostgreSQLConnection");
        // AddDbContextをサービスコレクションに登録する
        services.AddDbContext<AppDbContext>(options =>
        {
            // データベース操作ログをデバッグレベルでコンソールに出力する
            options.LogTo(Console.WriteLine, LogLevel.Debug);
            // PostgreSQLのデータベースを指定された接続文字列を使用して構成
            options.UseNpgsql(connectstr);
        });

        // ドメインオブジェクト:BookSctockとBookStockEntityの相互変換クラス
        services.AddScoped<BookStockEntityAdapter>();
        // ドメインオブジェクト:BookCategoryとBookCategoryEntityの相互変換クラス
        services.AddScoped<BookCategoryEntityAdapter>();
        // ドメインオブジェクト:BookとBookEntityの相互変換クラス
        services.AddScoped<BookEntityAdapter>();
        // 商品、商品カテゴリ、商品在庫オブジェクトの相互変換Factoryクラス
        services.AddScoped<BookFactory>();

        // ドメインオブジェクト:商品カテゴリのCRUD操作Repositoryインターフェイス
        services.AddScoped<IBookCategoryRepository, BookCategoryRepository>();
        // ドメインオブジェクト:商品のCRUD操作Repositoryインターフェイス
        services.AddScoped<IBookRepository, BookRepository>();
        // Unit of Workパターンを利用したトランザクション制御インターフェイス
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // ドメインオブジェクト:UserとUserEntityの相互変換クラス
        services.AddScoped<UserEntityAdapter>();
        // ドメインオブジェクト:User(ユーザー)のCRUD操作インターフェイスの実装
        services.AddScoped<IUserRepository, UserRepository>();
        // JWTの発行・検証インターフェイスの実装
        services.AddSingleton<IJwtTokenProvider, JwtTokenProvider>();

        return services;

    }

    /// <summary>
    /// アプリケーション層の依存関係を追加
    /// </summary>
    /// <param name="services">依存関係注入(DI)のサービスコレクション</param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static IServiceCollection AddApplicationLayerDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IRegisterBookUsecase, RegisterBookUsecase>();
        services.AddScoped<IUpdateBookUsecase, UpdateBookUsecase>();
        services.AddScoped<ISearchBookByKeywordUsecase, SearchBookByKeywordUsecase>();
        services.AddScoped<IGetBookInfoByBookIdUsecase, GetBookInfoByBookIdUsecase>();
        services.AddScoped<IDeleteUsecase, DeleteBookUsecase>();
        // ASP.NET Core Identityのパスワードハッシュ化・検証機能
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        // PBKDF2アルゴリズムを利用したパスワードハッシュ化・検証機能
        services.AddScoped<IPasswordHashingService, PBKDF2PasswordHashingService>();
        // ユースケース:[ユーザーを登録する]を実現するインターフェイス
        services.AddScoped<IRegisterUserUsecase, RegisterUserUsecase>();
        // JwtSettingsをバインドしてDIに登録する
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        // ユースケース:[ログインする]を実現するインターフェイス
        services.AddScoped<IAuthenticateUserUsecase, AuthenticateUserUsecase>();
        return services;
    }

    /// <summary>
    /// プレゼンテーション層の依存関係を追加
    /// </summary>
    /// <param name="services">依存関係注入(DI)のサービスコレクション</param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static IServiceCollection AddPresentationLayerDependencies(
        this IServiceCollection services, IConfiguration config)
    {
        // コントローラをサービスコレクションに登録する
        services.AddControllers();

        // RegisterBookViewModelからドメインオブジェクト:Bookへ変換するアダプタ
        services.AddScoped<RegisterBookViewModelAdapter>();
        services.AddScoped<UpdateBookViewModelAdapter>();
        // RegisterUserViewModelからドメインオブジェクト:Userへ変換するアダプタ
        services.AddScoped<RegisterUserViewModelAdapter>();

        return services;
    }

    /// <summary>
    /// テストプロジェクトにServiceProviderを提供するヘルパメソッド
    /// </summary>
    /// <param name="config"></param>
    /// <param name="configureServices"></param>
    /// <param name="configureLogging"></param>
    /// <returns></returns>
    public static ServiceProvider BuildAppProvider(
       IConfiguration config,
       Action<IServiceCollection>? configureServices = null,
       Action<ILoggingBuilder>? configureLogging = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            if (configureLogging is not null) configureLogging(b);
            else b.AddConsole().SetMinimumLevel(LogLevel.Warning);
        });
        services.AddApplicationDependencies(config);
        configureServices?.Invoke(services);

        return services.BuildServiceProvider(validateScopes: true);
    }
}