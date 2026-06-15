using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryApi.Domains.Models;

namespace LibraryApi.Domains.Repositories;

public interface IBookRepository
{
    Task CreateAsync(Book book);
    Task<Book?> SelectByIdWithBookStockAndBookCategoryAsync(string id);
    Task<List<Book>> SelectByNameLikeWithBookStockAndBookCategoryAsync(string name);
    Task<Book?> UpdateByIdAsync(Book book);
    Task<bool> DeleteByIdAsync(string id);
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByIdAsync(string id);
    Task<Book> SelectByNameWithIdAsync(string name);

}
