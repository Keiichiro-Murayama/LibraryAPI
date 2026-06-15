using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryApi.Applications.Usecases.Books.Interfaces;
using LibraryApi.Domains.Models;
using Swashbuckle.AspNetCore.Annotations;

using Microsoft.AspNetCore.Mvc;
using LibraryApi.Applications.Exceptions;
using System.Security.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace LibraryApi.Presentations.Controllers;

[ApiController]
[Route("library/api/books")]
[SwaggerTag("図書詳細情報のID検索API")]
public class GetBookInfoByBookIdController : ControllerBase
{
    // フィールド
    private readonly IGetBookInfoByBookIdUsecase _usecase;

    public GetBookInfoByBookIdController(IGetBookInfoByBookIdUsecase usecase)
    {
        _usecase = usecase;
    }

    /// <summary>
    /// BookIdで図書情報を取得する
    /// </summary>
    ///BookIdで図書情報を取得する
    [Authorize]
    [HttpGet("{bookId}")]
    [SwaggerResponse(StatusCodes.Status200OK, "図書の詳細取得成功", typeof(Book))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "未認証、またはJWTトークン無効")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "指定された図書が存在しない")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "サーバー内部エラー")]
    public async Task<IActionResult> GetInfo(string bookId)
    {
        try
        {
            var result = await _usecase.GetInfoAsync(bookId);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(new{ code = "BookNotFound", message= e.Message});
        }
    }
}
