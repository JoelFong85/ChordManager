# ChordManager

Chord Manager is a library to save chords & upload audio files for the chords

## Requirements

- .NET version 4.6


## Installation

- Open solution in Visual Studio.
- Build solution (ctrl-shift-B). Dependencies will be downloaded for the build.
- Run project in IIS Express (Google Chrome preferred).

## Database
Database can be found in App_Data > AudioDump.mdf 

AudioDump.mdf can be queried via VS' server explorer tab.

## Postman Collections for Testing
Postman collection for testing is available: ChordManager_Postman_Collection.json

## MP3 files for testing
MP3 files for testing can be found in folder AudioFileDump. 

The AddChord & UpdateChord APIs can take an mp3 file and upload it to the AudioFileDump folder.

There is a file transfer limit size of 10mb.

## Naming Conventions
Examples of chord names to pass to APIs:
- C major / C# minor / Bb major

