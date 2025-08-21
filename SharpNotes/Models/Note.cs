using System;
using System.Collections.Generic;

namespace SharpNotes.Models;

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsFavorite { get; set; }
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
}