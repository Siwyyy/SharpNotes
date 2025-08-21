namespace SharpNotes.Models;

public class NoteTag
{
    public int NoteId { get; set; }
    public required Note Note { get; set; }

    public int TagId { get; set; }
    public required Tag Tag { get; set; }
}