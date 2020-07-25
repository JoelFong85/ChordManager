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

        public struct ErrorMessages
        {
            public const string FieldEmpty = "Please ensure all fields are filled up before submitting";
            public const string InvalidNote = "Please enter a valid note";
            public const string NoAudioFile = "Please append an audio file to your request";
            public const string ChordExists = "The chord already exists";
            public const string AudioFileExists = "The audio file already exists";
        }

        public struct SuccessMessages
        {
            public const string FileUploaded = "File uploaded";
        }

        public const string NoteFieldName = "Note";

    }
}