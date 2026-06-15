using LibraryApi.Applications.Exceptions;

namespace LibraryApi.Presentations.Middlewares;
/// <summary>
/// InternalException及び未対応の例外をハンドリングするミドルウェア
/// - 例外をキャッチし、ログにエラーメッセージとスタックトレースを出力する
/// - クライアントには統一的な JSON レスポンスを返し、内部情報を直接漏らさない
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="next">次のミドルウェアを実行するためのデリゲート</param>
    /// <param name="logger">>ログ出力のためのILoggerインスタンス</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// ミドルウェア本体
    /// - try/catch で次の処理をラップし、例外発生時にキャッチする
    /// - InternalException とそれ以外の例外を区別して処理する
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 次のミドルウェアやコントローラを実行
            await _next(context);
        }
        catch (InternalException ex)
        {
            // InternalException(内部エラー)、StackTraceも含めてログ出力
            _logger.LogError(ex.Message, ex , ex.StackTrace);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            // クライアントには詳細を隠してシンプルなレスポンスを返す
            await context.Response.WriteAsJsonAsync(
                new { error = "現在サービスを停止しています。", message = ex.Message });
        }
        catch (Exception ex)
        {
            // 未処理の例外(予期しないエラー)、StackTrace をログに出力
            _logger.LogError(ex, "Unhandled exception occurred.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            // クライアントには内部情報を返さない
            await context.Response.WriteAsJsonAsync(
                new { error = "現在サービスを停止しています。" });
        }
    }
}