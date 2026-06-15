using LibraryApi.Domains.Models;
using LibraryApi.Domains.Repositories;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Applications.Usecases.Books.Interfaces;
namespace LibraryApi.Applications.Usecases.Books.Interactors;
/// <summary>
/// ユースケース:[図書を変更する]を実現するインターフェイスの実装
/// </summary>
public class UpdateBookUsecase : IUpdateBookUsecase
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="bookRepository">図書CRUD操作リポジトリ</param>
    /// <param name="unitOfWork">トランザクション制御機能</param>
    public UpdateBookUsecase(
        IBookRepository bookRepository, IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// 指定ざれた図書の存在有無を調べる
    /// </summary>
    /// <param name="bookName">図書目</param>
    /// <returns>なし</returns>
    /// <exception cref="ExistsException">同一図書名が存在する場合にスローされる</exception>
    public async Task ExistsByBookNameAsync(string id)
    {
        // 指定された図書の有無を調べる
        var result = await _bookRepository.ExistsByNameAsync(id);
        if (result is false) // 図書が既に存在する
        {
            throw new ExistsException($"図書名:{id}は存在しません。");
        }
    }

    /// <summary>
    /// 図書を変更するする
    /// </summary>
    /// <param name="book">変更対象対象図書</param>
    /// <returns>なし</returns>
    /// <exception cref="NotFoundException">図書が存在しない場合にスローされる</exception>
    public async Task<Book?> UpdateBookAsync(Book book)
    {
        // トランザクションを開始する
        await _unitOfWork.BeginAsync();
        try
        {
            var result = await _bookRepository.UpdateByIdAsync(book);
            if (result is null)
            {
                throw new NotFoundException($"図書Id:{book.Title}の図書は存在しないため変更できません。");
            }
            // トランザクションをコミットする
            await _unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            // トランザクションをロールバッする
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}