using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WhisperVoice.Models;
using WhisperVoice.Services;

namespace WhisperVoice.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    private readonly IConfigService _config;

    [ObservableProperty] private string _editorText = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isEditing;
    private long? _editingId;

    public ObservableCollection<NoteEntry> Notes { get; } = [];
    public ObservableCollection<NoteEntry> FilteredNotes { get; } = [];

    public NotesViewModel(IConfigService config)
    {
        _config = config;
        foreach (var note in config.Current.Notes)
            Notes.Add(note);
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void SaveNote()
    {
        var text = EditorText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (_editingId is not null)
        {
            var idx = Notes.ToList().FindIndex(n => n.Id == _editingId);
            if (idx >= 0)
            {
                Notes[idx] = Notes[idx] with { Text = text, EditedAt = DateTime.Now };
                Log.Debug("Note {Id} updated", _editingId);
            }
            CancelEdit();
        }
        else
        {
            var note = new NoteEntry { Text = text };
            Notes.Insert(0, note);
            Log.Debug("Note created: {Id}", note.Id);
        }

        EditorText = "";
        PersistNotes();
        ApplyFilter();
    }

    [RelayCommand]
    private void EditNote(NoteEntry note)
    {
        _editingId = note.Id;
        EditorText = note.Text;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingId = null;
        EditorText = "";
        IsEditing = false;
    }

    [RelayCommand]
    private void DeleteNote(NoteEntry note)
    {
        Notes.Remove(note);
        if (_editingId == note.Id)
            CancelEdit();
        PersistNotes();
        ApplyFilter();
        Log.Debug("Note {Id} deleted", note.Id);
    }

    private void PersistNotes()
    {
        _config.Update(c => c with { Notes = Notes.ToList() });
    }

    private void ApplyFilter()
    {
        FilteredNotes.Clear();
        var query = SearchText.Trim();
        var source = string.IsNullOrEmpty(query)
            ? Notes
            : new ObservableCollection<NoteEntry>(
                Notes.Where(n => n.Text.Contains(query, StringComparison.OrdinalIgnoreCase)));
        foreach (var n in source)
            FilteredNotes.Add(n);
    }
}
