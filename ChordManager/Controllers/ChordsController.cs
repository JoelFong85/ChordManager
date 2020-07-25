using ChordManager.Models;
using ChordManager.Models.Chords;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Http;

namespace ChordManager.Controllers
{
    public class ChordsController : ApiController
    {
        //Will save the audio file to the AudioFileDump folder in this project. Other options include saving to another directory in the server / pc, or to an external file system.
        string audioFileDumpPath = AppDomain.CurrentDomain.BaseDirectory + ApplicationConstants.AudioFileDumpPath;

        [Route("api/Chords/AddChord")]
        [HttpPost]
        public async Task<HttpResponseMessage> AddChord()
        {
            // Load MultipartFormDataStreamProvider
            var provider = new MultipartFormDataStreamProvider(audioFileDumpPath);
            await Request.Content.ReadAsMultipartAsync(provider);

            var addChordRequestModel = new Chord();

            //Validate data passed into API
            try
            {
                // Map form data to model
                addChordRequestModel = MapChordModel(provider);

                //Return 400 bad request if any of the notes are empty
                if (string.IsNullOrEmpty(addChordRequestModel.ChordName) || string.IsNullOrEmpty(addChordRequestModel.Note1) || string.IsNullOrEmpty(addChordRequestModel.Note2) || string.IsNullOrEmpty(addChordRequestModel.Note3))
                    throw new Exception(ApplicationConstants.ErrorMessages.FieldEmpty);

                ValidateChordModelNotes(addChordRequestModel);

                // Validate FileData exists                              
                if (provider.FileData == null || provider.FileData.Count <= 0)
                    throw new Exception(ApplicationConstants.ErrorMessages.NoAudioFile);
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.BadRequest, ex.Message).Pack();
            }

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    // Save data to DB
                    using (AudioDumpEntities context = new AudioDumpEntities())
                    {
                        if (context.T_Chord.Any(m => m.Chord_Name.ToUpper().Equals(addChordRequestModel.ChordName) && m.Is_Valid == true))
                            throw new Exception(ApplicationConstants.ErrorMessages.ChordExists);

                        T_Chord newChord = new T_Chord()
                        {
                            Chord_Name = addChordRequestModel.ChordName,
                            Note_1 = addChordRequestModel.Note1,
                            Note_2 = addChordRequestModel.Note2,
                            Note_3 = addChordRequestModel.Note3,
                            Is_Valid = true
                        };

                        newChord.Audio_File_Ext = SaveAudioFile(provider, addChordRequestModel.ChordName, true);

                        context.T_Chord.Add(newChord);
                        context.SaveChanges();
                    }

                    // Save audio file to AudioFileDump folder                    


                    scope.Complete(); // only complete transaction if the audioFile is properly saved
                }
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.InternalServerError, ex.Message).Pack();
            }

            return new PackedResponse(HttpStatusCode.OK, ApplicationConstants.SuccessMessages.ChordAdded).Pack();
        }

        private Chord MapChordModel(MultipartFormDataStreamProvider provider)
        {
            Chord result = new Chord();

            try
            {
                //If form field is not passed in, will have null object exception. Return 400 bad request.
                result.ChordName = provider.FormData.GetValues("ChordName")[0].Replace(" ", "").ToUpper();
                result.Note1 = provider.FormData.GetValues("Note1")[0].ToUpper();
                result.Note2 = provider.FormData.GetValues("Note2")[0].ToUpper();
                result.Note3 = provider.FormData.GetValues("Note3")[0].ToUpper();
            }
            catch (Exception ex)
            {
                throw new Exception(ApplicationConstants.ErrorMessages.FieldEmpty);
            }

            return result;
        }

        private void ValidateChordModelNotes(Chord chord)
        {
            //Validate if notes passed in are musical notes.
            foreach (PropertyInfo propertyInfo in chord.GetType().GetProperties())
            {
                if (propertyInfo.Name.Contains(ApplicationConstants.NoteFieldName))
                {
                    var fieldValue = chord.GetType().GetProperty(propertyInfo.Name).GetValue(chord, null);
                    if (!string.IsNullOrEmpty(fieldValue.ToString()) && !ApplicationConstants.NotesInScale.Contains(fieldValue.ToString().ToUpper()))
                        throw new Exception($@"{ApplicationConstants.ErrorMessages.InvalidNote} for {propertyInfo.Name}");
                }
            }
        }

        private string SaveAudioFile(MultipartFormDataStreamProvider provider, string chordName, bool audioFileMandatory)
        {
            var file = provider.FileData[0];
            var fileExtension = file.Headers.ContentDisposition.FileName.Trim('"').Split('.').Last();
            if (string.IsNullOrEmpty(fileExtension) || !ApplicationConstants.AudioFileFormats.Contains(fileExtension))
            {
                if (audioFileMandatory)
                    throw new Exception(ApplicationConstants.ErrorMessages.WrongAudioFileFormat);
                else //for update api, not mandatory to pass in audio file if user doesn't want to update it
                    return "";
            }

            var tempFileName = file.LocalFileName;
            var targetFilePath = Path.Combine(audioFileDumpPath, $@"{chordName}.{fileExtension}");

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            File.Move(tempFileName, targetFilePath);

            return fileExtension;
        }

        [Route("api/Chords/ListAllChords")]
        [HttpGet]
        public List<Chord> ListAllChords()
        {
            List<Chord> chordList = new List<Chord>();

            using (AudioDumpEntities context = new AudioDumpEntities())
            {
                chordList = context.T_Chord.Where(m => m.Is_Valid == true)
                    .Select(x =>
                    new Chord
                    {
                        ChordName = x.Chord_Name,
                        Note1 = x.Note_1,
                        Note2 = x.Note_2,
                        Note3 = x.Note_3
                    }).ToList();
            }

            return chordList;
        }

        [Route("api/Chords/GetChordByName")]
        [HttpPost]
        public HttpResponseMessage GetChordByName([FromBody] GetChordByNameRequestModel model)
        {
            if (string.IsNullOrEmpty(model.ChordName))
                return new PackedResponse(HttpStatusCode.BadRequest, ApplicationConstants.ErrorMessages.ChordNameEmpty).Pack();

            Chord result = new Chord();

            using (AudioDumpEntities context = new AudioDumpEntities())
            {
                model.ChordName = model.ChordName.Replace(" ", "").ToUpper();
                var chordRecord = context.T_Chord.FirstOrDefault(m => m.Chord_Name.Equals(model.ChordName) && m.Is_Valid == true);

                if (chordRecord == null)
                    return new PackedResponse(HttpStatusCode.BadRequest, ApplicationConstants.ErrorMessages.ChordDoesNotExist).Pack();

                result.ChordName = chordRecord.Chord_Name;
                result.Note1 = chordRecord.Note_1;
                result.Note2 = chordRecord.Note_2;
                result.Note3 = chordRecord.Note_3;
            }

            return new PackedResponse(HttpStatusCode.OK, Newtonsoft.Json.JsonConvert.SerializeObject(result)).PackAsJson();
        }

        [Route("api/Chords/UpdateChord")]
        [HttpPut]
        public async Task<HttpResponseMessage> UpdateChord()
        {
            // Load MultipartFormDataStreamProvider
            var provider = new MultipartFormDataStreamProvider(audioFileDumpPath);
            await Request.Content.ReadAsMultipartAsync(provider);

            var updateChordRequestModel = new Chord();

            try
            {
                // Map form data to model
                updateChordRequestModel = MapChordModel(provider);

                ValidateChordModelNotes(updateChordRequestModel);
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.BadRequest, ex.Message).Pack();
            }

            try
            {
                using (AudioDumpEntities context = new AudioDumpEntities())
                {
                    var chordRecord = context.T_Chord.FirstOrDefault(m => m.Chord_Name.Equals(updateChordRequestModel.ChordName) && m.Is_Valid == true);

                    //validate if chord exists
                    if (chordRecord == null)
                        throw new Exception(ApplicationConstants.ErrorMessages.ChordDoesNotExist);

                    if (!string.IsNullOrEmpty(updateChordRequestModel.Note1))
                        chordRecord.Note_1 = updateChordRequestModel.Note1;

                    if (!string.IsNullOrEmpty(updateChordRequestModel.Note2))
                        chordRecord.Note_2 = updateChordRequestModel.Note2;

                    if (!string.IsNullOrEmpty(updateChordRequestModel.Note3))
                        chordRecord.Note_3 = updateChordRequestModel.Note3;

                    var fileExtension = string.Empty;

                    // Check if there is a audio file for updating
                    if (provider.FileData != null && provider.FileData.Count >= 0)
                    {
                        fileExtension = SaveAudioFile(provider, updateChordRequestModel.ChordName, false);
                    }

                    if (!string.IsNullOrEmpty(fileExtension))
                        chordRecord.Audio_File_Ext = fileExtension;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.InternalServerError, ex.Message).Pack();
            }

            return new PackedResponse(HttpStatusCode.OK, ApplicationConstants.SuccessMessages.ChordUpdated).Pack();
        }

        [Route("api/Chords/DeleteChord")]
        [HttpDelete]
        public HttpResponseMessage DeleteChord([FromBody] DeleteChordRequestModel model)
        {
            if (string.IsNullOrEmpty(model.ChordName))
                return new PackedResponse(HttpStatusCode.BadRequest, ApplicationConstants.ErrorMessages.ChordNameEmpty).Pack();

            try
            {
                model.ChordName = model.ChordName.Replace(" ", "").ToUpper();

                using (TransactionScope scope = new TransactionScope())
                {
                    using (AudioDumpEntities context = new AudioDumpEntities())
                    {
                        //check if chord exists in db
                        var chordRecord = context.T_Chord.FirstOrDefault(m => m.Chord_Name.Equals(model.ChordName) && m.Is_Valid == true);
                        if (chordRecord == null)
                            throw new Exception(ApplicationConstants.ErrorMessages.ChordDoesNotExist);

                        // if exist, set valid to false
                        chordRecord.Is_Valid = false;
                        context.SaveChanges();

                        //Delete audio file
                        var targetFilePath = Path.Combine(audioFileDumpPath, $@"{model.ChordName}.{chordRecord.Audio_File_Ext}");
                        if (File.Exists(targetFilePath))
                        {
                            File.Delete(targetFilePath);
                        }
                    }

                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.InternalServerError, ex.Message).Pack();
            }

            return new PackedResponse(HttpStatusCode.OK, ApplicationConstants.SuccessMessages.ChordDeleted).Pack();
        }

        [Route("api/Chords/DownloadAudioFileByChord")]
        [HttpGet]
        public HttpResponseMessage DownloadAudioFileByChord([FromBody] DownloadAudioFileByChordRequestModel model)
        {
            if (string.IsNullOrEmpty(model.ChordName))
                return new PackedResponse(HttpStatusCode.BadRequest, ApplicationConstants.ErrorMessages.ChordNameEmpty).Pack();

            try
            {
                model.ChordName = model.ChordName.Replace(" ", "").ToUpper();
                var audioFileExt = string.Empty;
                using (AudioDumpEntities context = new AudioDumpEntities())
                {
                    //check if chord exists in db
                    var chordRecord = context.T_Chord.FirstOrDefault(m => m.Chord_Name.Equals(model.ChordName) && m.Is_Valid == true);
                    if (chordRecord == null)
                        throw new Exception(ApplicationConstants.ErrorMessages.ChordDoesNotExist);

                    audioFileExt = chordRecord.Audio_File_Ext;
                }

                var targetFilePath = Path.Combine(audioFileDumpPath, $@"{model.ChordName}.{audioFileExt}");
                if (!File.Exists(targetFilePath))
                    throw new Exception(ApplicationConstants.ErrorMessages.AudioFileDoesNotExist);

                var dataBytes = File.ReadAllBytes(targetFilePath);
                var dataStream = new MemoryStream(dataBytes);

                return new PackedResponse(HttpStatusCode.OK, ApplicationConstants.SuccessMessages.DownloadSuccessful).PackAsDownload(dataStream, model.ChordName, audioFileExt);
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.InternalServerError, ex.Message).Pack();
            }
        }
    }
}
