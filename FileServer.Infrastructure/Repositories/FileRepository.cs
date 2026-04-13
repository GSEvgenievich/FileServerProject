using FileServer.Domain.Entities;
using FileServer.Domain.Interfaces;
using FileServer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileServer.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly AppDbContext _context;

    public FileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FileRecord?> GetByIdAsync(Guid id)
    {
        return await _context.FileRecords.FindAsync(id);
    }

    public async Task<IEnumerable<FileRecord>> GetAllAsync()
    {
        return await _context.FileRecords
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<FileRecord> AddAsync(FileRecord record)
    {
        await _context.FileRecords.AddAsync(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task DeleteAsync(Guid id)
    {
        var record = await _context.FileRecords.FindAsync(id);
        if (record != null)
        {
            _context.FileRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.FileRecords.AnyAsync(f => f.Id == id);
    }
}