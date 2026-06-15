using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryApi.Applications.Usecases.Users.Interfaces;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Domains.Models;
using LibraryApi.Presentations.Adapters;
using LibraryApi.Presentations.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using Swashbuckle.AspNetCore.Annotations;


namespace LibraryApi.Presentations.Controllers;

[ApiController]
[Route("library/api/users")]
public class RegisterUserController : ControllerBase
{
    //プロパティ
    public readonly IRegisterUserUsecase _usecase;
    public readonly RegisterUserViewModelAdapter _adapter;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="usecase"></param>
    /// <param name="adapter"></param>
    public RegisterUserController(IRegisterUserUsecase usecase, RegisterUserViewModelAdapter adapter)
    {
        _usecase = usecase;
        _adapter = adapter;
    }

    /// <summary>
    /// ユーザ登録
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [SwaggerOperation(Summary = "ユーザーを登録",
              Description = "ユーザー情報を受け取り、ユーザーを登録する")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "バリデーションエラーまたは業務ルール違反")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "ユーザーが既に存在する場合")]
    [SwaggerResponse(StatusCodes.Status201Created, "登録成功", typeof(User))]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequestViewModel request)
    {
        // サーバーサイドバリデーション
        if (!ModelState.IsValid)
        {
            // プロパティ名をキー、エラーメッセージ配列を値とするディクショナリに変換する
            var details = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0) // エラーがある項目だけを抽出する
                .ToDictionary( // Dictionaryに変換する
                               // キー:プロパティ名 ("Username", "Email" など)
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
            //重複確認
            await _usecase.ExistsByUsernameAsync(request.username);
            //オブジェクト変換
            var user = await _adapter.RestoreAsync(request);
            //登録
            await _usecase.RegisterUserAsync(user);
            var response = await _adapter.ConvertAsync(user);
            return Created("", response);
        }
        catch (ExistsException ex)
        {
            return Conflict(new { error = "DuplicateUsername", message = ex.Message });
        }
        catch (DomainException ex)
        {

            return BadRequest(new
            {
                error = "ValidationError",
                message = ex.Message
            });
        }


    }







}