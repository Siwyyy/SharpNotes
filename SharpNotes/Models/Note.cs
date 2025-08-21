using System;
using System.Collections.Generic;

namespace SharpNotes.Models;

public class Note
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsFavorite { get; set; }
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
}