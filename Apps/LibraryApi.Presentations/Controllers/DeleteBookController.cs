using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryApi.Domains.Models;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using LibraryApi.Presentations.Adapters;
using LibraryApi.Presentations.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
namespace LibraryApi.Presentations.Controllers;
/// <summary>
/// ユースケース:[図書を削除する]を実現するコントローラ
/// </summary>
[ApiController]
[Route("library/api/books")]
[SwaggerTag("図書削除API")]
public class DeleteBookController : ControllerBase
{
    private readonly IDeleteUsecase _usecase;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="usecase">ユースケース:[図書を削除する]を実現するインターフェイス</param>
    public DeleteBookController(
        IDeleteUsecase usecase)
    {
        _usecase = usecase;

    }

    /// <summary>
    /// 図書を削除する
    /// </summary>
    /// <param name="bookId">削除対象の図書Id(UUID)</param>
    /// <returns></returns>
    [Authorize]
    [HttpDelete("{bookId}")]
    [SwaggerOperation(
Summary = "図書削除",
Description = "図書情報を更新します。図書名の重複や存在しない図書Idを受け取った場合はエラーを返す"
)]
    [SwaggerResponse(StatusCodes.Status204NoContent, "削除成功")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "バリデーションエラーまたは業務ルール違反")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "指定された図書Idが存在しない場合")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "図書名が既に存在する場合")]
    public async Task<IActionResult> Deleted([FromRoute] string bookId)
    {
        // サーバーサイドバリデーション
        if (!ModelState.IsValid)
        {
            // プロパティ名をキー、エラーメッセージ配列を値とするディクショナリに変換する
            var details = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0) // エラーがある項目だけを抽出する
                .ToDictionary( // Dictionaryに変換する
                               // キー:プロパティ名 ("Name", "Price" など)
                    kv => kv.Key,
                    // 値: 当該プロパティのエラーメッセージ一覧
                    kv => kv.Value!.Errors
                        // エラーメッセージが空やnullの場合は "Invalid value."に置換する
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "Invalid value." : e.ErrorMessage)
                        .ToArray()
                );
            return BadRequest(new
            { code = "VALIDATION_ERROR", message = "入力内容に誤りがあります。", details });
        }
        try
        {
            var book = await _usecase.GetBookByIdAsync(bookId);
            // 図書を削除する
            await _usecase.DeleteBookAsync(book);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { code = "PRODUCT_NOT_FOUND", message = ex.Message });
        }
        catch (ExistsException ex)
        {
            // エラーレスポンスを返却する
            return NotFound(
                new { code = "PRODUCT_NOT_FOUND", message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(
                new { code = "DOMAIN_RULE_VIOLATION", message = ex.Message });
        }
    }
}