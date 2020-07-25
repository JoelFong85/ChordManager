using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace ChordManager
{
    public class ApplicationConstants
    {
        public const string AudioFileDumpPath = "\\AudioFileDump\\";

        public static readonly IList<String> NotesInScale = new ReadOnlyCollection<string>(new List<String>
        {
            "C",
            "C#", "Db",
            "D",
            "D#", "Eb",
            "E",
            "F",
            "F#", "Gb",
            "G",
            "G#", "Ab",
            "A",
            "A#", "Bb",
            "B"
        });

        public static readonly IList<String> AudioFileFormats = new ReadOnlyCollection<string>(new List<String>
        {
            "mp3",
            "wav",
            "3gp",
            "flac",
            "wma"
        });

        public struct ErrorMessages
        {
            public const string FieldEmpty = "Please ensure all fields are filled up before submitting";
            public const string InvalidNote = "Please enter a valid note";
            public const string NoAudioFile = "Please append an audio file to your request";
            public const string ChordNameEmpty = "Please submit a valid Chord Name with your request";
            public const string ChordExists = "The chord already exists";
            public const string AudioFileExists = "The audio file already exists";
            public const string AudioFileDoesNotExist = "The audio file does not exist";
            public const string ChordDoesNotExist = "The chord does not exist";
            public const string WrongAudioFileFormat = "Please upload the audio file in the following formats (mp3, wav, 3gp, flac, wma)";
        }

        public struct SuccessMessages
        {
            public const string ChordAdded = "Chord has been added";
            public const string ChordUpdated = "Chord has been updated";
            public const string ChordDeleted = "Chord has been deleted";
            public const string DownloadSuccessful = "Your download is successful";            
        }

        public const string NoteFieldName = "Note";

    }
}
