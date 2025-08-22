using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SharpNotes.Database;
using SharpNotes.Models;

namespace SharpNotes.Services;

public class NotesService
{
    private readonly NotesDbContext _dbContext;

    public NotesService(NotesDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<List<Note>> GetAllNotesAsync()
    {
        return await _dbContext.Notes
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .ToListAsync();
    }

    public async Task<Note> GetNoteByIdAsync(int id)
    {
        return await _dbContext.Notes
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .FirstOrDefaultAsync(n => n.Id == id) ?? throw new InvalidOperationException();
    }

    public async Task<List<Note>> SearchNotesAsync(string searchTerm)
    {
        return await _dbContext.Notes
            .Where(n => n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm))
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .ToListAsync();
    }

    public async Task<Note> AddNoteAsync(Note note)
    {
        note.CreatedAt = DateTime.Now;
        note.ModifiedAt = DateTime.Now;

        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync();
        return note;
    }

    public async Task UpdateNoteAsync(Note note)
    {
        note.ModifiedAt = DateTime.Now;

        _dbContext.Entry(note).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteNoteAsync(int id)
    {
        var note = await _dbContext.Notes.FindAsync(id);
        if (note != null)
        {
            _dbContext.Notes.Remove(note);
            await _dbContext.SaveChangesAsync();
        }
    }


    public async Task<List<Tag>> GetAllTagsAsync()
    {
        return await _dbContext.Tags.ToListAsync();
    }

    public async Task<Tag> AddTagAsync(string tagName)
    {
        var tag = new Tag { Name = tagName };
        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();
        return tag;
    }


    public async Task<Tag> GetTagByIdAsync(int id)
    {
        return await _dbContext.Tags.FindAsync(id) ?? throw new InvalidOperationException();
    }

    public async Task<Tag> GetOrCreateTagAsync(string tagName)
    {
        var tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
        if (tag == null)
        {
            tag = new Tag { Name = tagName };
            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();
        }

        return tag;
    }

    public async Task AddTagToNoteAsync(int noteId, int tagId)
    {
        var exists = await _dbContext.NoteTags
            .AnyAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

        if (!exists)
        {
            var note = await GetNoteByIdAsync(noteId);
            var tag = await GetTagByIdAsync(tagId);

            var noteTag = new NoteTag
            {
                Note = note,
                Tag = tag,
                NoteId = noteId,
                TagId = tagId
            };

            _dbContext.NoteTags.Add(noteTag);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveTagFromNoteAsync(int noteId, int tagId)
    {
        var noteTag = await _dbContext.NoteTags
            .FirstOrDefaultAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

        if (noteTag != null)
        {
            _dbContext.NoteTags.Remove(noteTag);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Note>> GetNotesByTagAsync(int tagId)
    {
        return await _dbContext.Notes
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .Where(n => n.NoteTags.Any(nt => nt.TagId == tagId))
            .ToListAsync();
    }

    public async Task DeleteTagAsync(int tagId)
    {
        var tag = await _dbContext.Tags.FindAsync(tagId);
        if (tag != null)
        {
            var noteTags = await _dbContext.NoteTags
                .Where(nt => nt.TagId == tagId)
                .ToListAsync();

            _dbContext.NoteTags.RemoveRange(noteTags);
            _dbContext.Tags.Remove(tag);
            await _dbContext.SaveChangesAsync();
        }
    }


    public async Task<Dictionary<int, double>> IndexNotesForSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes.ToListAsync(cancellationToken);

        Dictionary<int, double> searchResults = new Dictionary<int, double>();

        await Task.Run(() =>
        {
            Parallel.ForEach(notes, note =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                double score = 0;
                if (note.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score += 2;
                if (note.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score += 1;

                if (score > 0)
                {
                    lock (searchResults)
                    {
                        searchResults[note.Id] = score;
                    }
                }
            });
        }, cancellationToken);

        return searchResults;
    }
}