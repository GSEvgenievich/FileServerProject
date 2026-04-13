using FileServer.Domain.Entities;

namespace FileServer.Domain.Interfaces;

public interface IFileRepository
{
    Task<FileRecord?> GetByIdAsync(Guid id);
    Task<IEnumerable<FileRecord>> GetAllAsync();
    Task<FileRecord> AddAsync(FileRecord record);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}