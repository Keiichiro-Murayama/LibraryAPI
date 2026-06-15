using Microsoft.EntityFrameworkCore;
using LibraryApi.Domains.Models;
using LibraryApi.Domains.Repositories;
using LibraryApi.Domains.Exceptions;
using LibraryApi.Applications.Exceptions;
using LibraryApi.Infrastructures.Adapters;
using LibraryApi.Infrastructures.Contexts;
namespace LibraryApi.Infrastructures.Repositories;
/// <summary>
///  ドメインオブジェクト:図書のCRUD操作インターフェイスの実装
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;
    private readonly BookFactory _factory;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="context">アプリケーション用データベースコンテキスト</param>
    /// <param name="factory">図書、図書カテゴリ、図書在庫オブジェクトの相互変換Factoryクラス</param>
    public BookRepository(AppDbContext context, BookFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    /// <summary>
    /// 図書を永続化する
    /// </summary>
    /// <param name="book">永続化する図書</param>
    /// <returns>なし</returns>
    public async Task CreateAsync(Book book)
    {
        try
        {
            // 登録する図書の図書カテゴリを取得する
            var category = await _context.Categories
                .SingleOrDefaultAsync(c => c.CategoryUuid == book.Category!.CategoryUuid);
            if (category is null)
            {
                throw new Exception($"Id:{book.Category!.CategoryUuid}の図書カテゴリは存在しません。");
            }
            // BookをBookEntityに変換する
            var entity = await _factory.ConvertAsync(book);
            // 図書カテゴリの外部キーを設定する
            entity.BookCategory = null;
            entity.CategoryId = category.Id;
            // 図書を登録する
            await _context.Books.AddAsync(entity);
            // 登録した図書をデータベースに永続化する
            await _context.SaveChangesAsync();
        }
        catch (DomainException)
        {
            throw; // DomainException例外はそのまま再スローする
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException("図書の永続化中に予期しないエラーが発生しました。", ex);
        }
    }

    /// <summary>
    /// 指定された図書Idの図書と在庫、図書カテゴリを返す
    /// </summary>
    /// <param name="id">図書Id</param>
    /// <returns>Book または null</returns>
    public async Task<Book?> SelectByIdWithBookStockAndBookCategoryAsync(string id)
    {
        try
        {
            // 図書Id(UUID)で図書と在庫、図書カテゴリをジョインして取得する
            var entity = await _context.Books
                .AsNoTracking()
                .Include(p => p.BookCategory)
                .Include(p => p.BookStock)
                .SingleOrDefaultAsync(p => p.BookUuid == id);
            if (entity is null)
            {
                return null; // 該当図書が存在しない場合はnullを返す
            }
            // BookEntityの集約からBookの集約に復元する
            var book = await _factory.RestoreAsync(entity);
            return book;
        }
        catch (DomainException)
        {
            throw; // DomainException例外はそのまま再スローする
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Id:{id}の図書取得時に予期しないエラーが発生しました。", ex);
        }
    }

    /// <summary>
    /// 指定されたキーワードで図書を部分一致検索して図書と在庫、図書カテゴリを取得する
    /// </summary>
    /// <param name="keyword">検索キーワード</param>
    /// <returns>Prodyctのリスト</returns>
    public async Task<List<Book>> SelectByNameLikeWithBookStockAndBookCategoryAsync(string keyword)
    {
        try
        {
            // 引数のキーワードで図書と在庫を部分一致検索する
            var entities = await _context.Books
                .AsNoTracking()
                .Include(p => p.BookStock)
                .Include(p => p.BookCategory)
                .Where(p => EF.Functions.Like(p.Title, $"%{keyword}%"))
                .ToListAsync();
            // List<BookEntity>からList<Book>を復元する
            var books = await _factory.RestoreAsync(entities);
            return books;
        }
        catch (DomainException)
        {
            throw; // DomainException例外はそのまま再スローする
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"キーワード:{keyword}の図書取得時に予期しないエラーが発生しました。", ex);
        }
    }

    /// <summary>
    /// 図書を更新する
    /// </summary>
    /// <param name="book">更新対象の図書</param>
    /// <returns>true:更新成功 false:更新失敗</returns>
    public async Task<Book?> UpdateByIdAsync(Book book)
    {
        try
        {
            var entity = await _context.Books
            .Include(p => p.BookStock)
            .Include(p => p.BookCategory)
            .SingleOrDefaultAsync(p => p.BookUuid == book.BookUuid);
            if (entity is null)
            {
                return null;
            }
            // 図書名と単価を変更する
            entity.Title = book.Title;
            entity.Author = book.Author;
            // 在庫数を変更する
            entity.BookStock!.Stock = book.Stock!.Stock;
            // 変更データをデータベースに永続化する
            await _context.SaveChangesAsync();

            var result = await _factory.RestoreAsync(entity);
            return result;
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Id:{book.BookUuid}の図書変更中に予期しないエラーが発生しました。", ex);
        }
    }

    /// <summary>
    /// 図書を削除する
    /// </summary>
    /// <param name="id">削除対象の図書Id(UUID)</param>
    /// <returns>true:削除成功 false:削除失敗</returns>
    public async Task<bool> DeleteByIdAsync(string id)
    {
        try
        {
            // 削除対象の図書を取得する
            var entity = await _context.Books.SingleOrDefaultAsync(p => p.BookUuid == id);
            var bookStock = await _context.BookStocks.Where(s => s.BookId == entity!.Id).ToListAsync();
            if (bookStock.Any())
            {
                _context.BookStocks.RemoveRange(bookStock);
            }
            if (entity is null)
            {
                return false; // 該当図書が存在しない場合はfalseを返す
            }
            // 図書を削除する
            _context.Books.Remove(entity);
            // 削除結果をデータベースに反映させる
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Id:{id}の図書削除中に予期しないエラーが発生しました。", ex);
        }
    }

    /// <summary>
    /// 指定された図書名の存在有無を返す
    /// </summary>
    /// <param name="name">図書名</param>
    /// <returns>true:存在する false:存在しない</returns> 
    public async Task<bool> ExistsByNameAsync(string name)
    {
        try
        {
            return await _context.Books
            .AsNoTracking()
            .AnyAsync(p => p.Title == name);
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Name:{name}の図書有無取得時に予期しないエラーが発生しました。", ex);
        }
    }

        /// <summary>
    /// 指定された図書IDの存在有無を返す
    /// </summary>
    /// <param name="name">図書名</param>
    /// <returns>true:存在する false:存在しない</returns> 
    public async Task<bool> ExistsByIdAsync(string id)
    {
        try
        {
            return await _context.Books
            .AsNoTracking()
            .AnyAsync(p => p.BookUuid == id);
        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Name:{id}の図書有無取得時に予期しないエラーが発生しました。", ex);
        }
    }

    public async Task<Book> SelectByNameWithIdAsync(string name)
    {
        try
        {
            var entity = await _context.Books
            .AsNoTracking()
            .Include(b => b.BookCategory)
            .Include(b => b.BookStock)
            .SingleOrDefaultAsync(p => p.Title == name);
            if (entity is null)
            {
                throw new NotFoundException("指定された図書が存在しません");
            }
            var obj = await _factory.RestoreAsync(entity);
            return obj;

        }
        catch (Exception ex)
        {
            // InternalExceptionにラップしてスローする
            throw new InternalException($"Name:{name}の図書有無取得時に予期しないエラーが発生しました。", ex);
        }
    }
}