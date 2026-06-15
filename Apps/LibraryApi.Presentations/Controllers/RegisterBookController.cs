using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryApi.Domains.Models;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Presentations.Adapters;
using LibraryApi.Presentations.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
namespace LibraryApi.Presentations.Controllers;
/// <summary>
/// ユースケース:[新図書を登録する]を実現するコントローラ
/// </summary>
[ApiController]
[Route("libraryapi/books")]
[SwaggerTag("新図書登録API")]
public class RegisterBookController : ControllerBase
{
    private readonly IRegisterBookUsecase _usecase;
    private readonly RegisterBookViewModelAdapter _adapter;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="usecase">ユースケース:[新図書を登録する]を実現するインターフェイス</param>
    /// <param name="adapter">RegisterBookViewModelからドメインオブジェクト:Bookへ変換するアダプタ</param>
    public RegisterBookController(
        IRegisterBookUsecase usecase,
        RegisterBookViewModelAdapter adapter)
    {
        _usecase = usecase;
        _adapter = adapter;
    }

    /// <summary>
    /// 図書カテゴリ一覧の取得
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("/api/categories")]
    [SwaggerOperation(Summary = "図書カテゴリ一覧を取得",
                          Description = "登録可能なすべての図書カテゴリを返します。")]
    [SwaggerResponse(StatusCodes.Status200OK, "カテゴリ一覧", typeof(List<BookCategory>))]
    public async Task<IActionResult> GetCategoriesAsync()
    {
        var result = await _usecase.GetCategoriesAsync();
        return Ok(result);
    }

    /// <summary>
    /// 新図書を登録する
    /// </summary>
    /// <param name="model">図書登録用ViewModel</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost]
    [SwaggerOperation(Summary = "新図書を登録",
              Description = "図書情報を受け取り、図書を登録する")]
    [SwaggerResponse(StatusCodes.Status201Created, "登録成功", typeof(Book))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "バリデーションエラーまたは業務ルール違反")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "カテゴリIdが存在しない場合")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "図書が既に存在する場合")]
    public async Task<IActionResult> Register(
// SwaggerRequestBodyを追加
[FromBody, SwaggerRequestBody("新図書登録用ViewModel", Required = true)]
        RegisterBookRequestViewModel model)
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
            { error = "ValidationError", message = "入力内容に誤りがあります。", details });
        }
        try
        {
            // 存在しない図書カテゴリを受信した(ミスしている)
            var category = await _usecase.GetCategoryByIdAsync(model.CategoryId);
            // 既に登録済みの図書を受信した(ミスしている)
            await _usecase.ExistsByBookNameAsync(model.Title);
            // RegisterBookViewModelからBookを復元する
            var fixedModel = await _adapter.TransAsync(model);
            fixedModel.CategoryName = category.Name;
            var book = await _adapter.RestoreAsync(fixedModel);
            

            // 図書を永続化する
            await _usecase.RegisterBookAsync(book);
            return Created($"/api/books/{book.BookUuid}", book.BookUuid);
        }
        catch (ExistsException ex)
        {
            // 既に存在する図書を受信した
            return Conflict(new { error = "ProductAlreadyExists", message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            // 存在しない図書カテゴリIdを受信した
            return BadRequest(new { error = "CategoryNotFound", message = ex.Message });
        }
        catch (DomainException ex)
        {
            // 業務ルール違反のデータを受信した
            return BadRequest(new { error = "ValidationError", message = ex.Message });
        }
    }

}