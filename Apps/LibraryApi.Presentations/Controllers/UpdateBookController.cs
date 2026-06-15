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
/// ユースケース:[図書を変更する]を実現するコントローラ
/// </summary>
[ApiController]
[Route("library/api/books")]
[SwaggerTag("図書変更API")]
public class UpdateBookController : ControllerBase
{
    private readonly IUpdateBookUsecase _usecase;
    private readonly UpdateBookViewModelAdapter _adapter;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="usecase">ユースケース:[図書を変更する]を実現するインターフェイス</param>
    /// <param name="adapter">UpdateBookViewModelからドメインオブジェクト:Bookへ変換するアダプタ</param>
    public UpdateBookController(
        IUpdateBookUsecase usecase,
        UpdateBookViewModelAdapter adapter)
    {
        _usecase = usecase;
        _adapter = adapter;
    }

    /// <summary>
    /// 図書を変更する
    /// </summary>
    /// <param name="model">図書変更用ViewModel</param>
    /// <param name="bookId">変更対象の図書Id(UUID)</param>
    /// <returns></returns>
    [Authorize]
    [HttpPut("{bookId}")]
    [SwaggerOperation(
Summary = "図書変更",
Description = "図書情報を更新します。図書名の重複や存在しない図書Idを受け取った場合はエラーを返す"
)]
    [SwaggerResponse(StatusCodes.Status200OK, "変更成功", typeof(Book))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "バリデーションエラーまたは業務ルール違反")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "指定された図書Idが存在しない場合")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "図書名が既に存在する場合")]
    public async Task<IActionResult> Updated([FromBody] UpdateBookRequestViewModel model, [FromRoute] string bookId)
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
            // 図書名の存在有無を調べる
            await _usecase.ExistsByBookNameAsync(bookId);
            // UpdateBookViewModelからBookを復元する
            var book = await _adapter.TransAsync(model, bookId);
            // 図書を変更する
            var result = await _usecase.UpdateBookAsync(book);
            var response = await _adapter.ConvertAsync(result);
            return Ok(response);
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