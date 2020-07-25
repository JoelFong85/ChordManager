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
        [HttpPost]
        public async Task<HttpResponseMessage> AddChord()
        {
            //Will save the audio file to the AudioFileDump folder in this project. Other options include saving to another directory in the server / pc, or to an external file system.
            string audioFileDumpPath = AppDomain.CurrentDomain.BaseDirectory + ApplicationConstants.AudioFileDumpPath;

            // Load MultipartFormDataStreamProvider
            var provider = new MultipartFormDataStreamProvider(audioFileDumpPath);
            await Request.Content.ReadAsMultipartAsync(provider);

            var addChordRequestModel = new Chord();

            //Validate data passed into API
            try
            {
                // Map form data to model
                addChordRequestModel = MapChordModel(provider);

                // Validate FileData
                if (provider.FileData == null || provider.FileData.Count <= 0)
                    throw new Exception(ApplicationConstants.ErrorMessages.NoAudioFile); // put in application constants
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.BadRequest, ex.Message).Pack();
            }

            try
            {
                // Save data to DB
                using (TransactionScope scope = new TransactionScope())
                {
                    using (AudioDumpEntities context = new AudioDumpEntities())
                    {
                        if (context.T_Chord.Any(x => x.Chord_Name.ToUpper().Equals(addChordRequestModel.ChordName)))
                            throw new Exception(ApplicationConstants.ErrorMessages.ChordExists);

                        context.T_Chord.Add(new T_Chord()
                        {
                            Chord_Name = addChordRequestModel.ChordName,
                            Note_1 = addChordRequestModel.Note1,
                            Note_2 = addChordRequestModel.Note2,
                            Note_3 = addChordRequestModel.Note3,
                            Is_Valid = true
                        }); ;
                        context.SaveChanges();
                    }

                    // Save audio file to AudioFileDump folder                    
                    var file = provider.FileData[0];
                    var fileExtension = file.Headers.ContentDisposition.FileName.Trim('"').Split('.').Last();
                    var tempFileName = file.LocalFileName;
                    var targetFilePath = Path.Combine(audioFileDumpPath, $@"{addChordRequestModel.ChordName}.{fileExtension}");

                    if (File.Exists(targetFilePath))
                        throw new Exception(ApplicationConstants.ErrorMessages.AudioFileExists);

                    File.Move(tempFileName, targetFilePath);

                    scope.Complete(); // only complete transaction if the audioFile is properly saved
                }
            }
            catch (Exception ex)
            {
                return new PackedResponse(HttpStatusCode.InternalServerError, ex.Message).Pack();
            }

            return new PackedResponse(HttpStatusCode.OK, ApplicationConstants.SuccessMessages.FileUploaded).Pack();
        }

        private Chord MapChordModel(MultipartFormDataStreamProvider provider)
        {
            Chord result = new Chord();

            try
            {
                //If form field is not passed in, will have null object exception. Return 400 bad request.
                result.ChordName = provider.FormData.GetValues("ChordName")[0].Replace(" ", "").ToUpper();
                result.Note1 = provider.FormData.GetValues("Note1")[0];
                result.Note2 = provider.FormData.GetValues("Note2")[0];
                result.Note3 = provider.FormData.GetValues("Note3")[0];

                //Return 400 bad request if any of the notes are empty
                if (string.IsNullOrEmpty(result.Note1) || string.IsNullOrEmpty(result.Note2) || string.IsNullOrEmpty(result.Note3))
                    throw new Exception(ApplicationConstants.ErrorMessages.FieldEmpty);
            }
            catch (Exception ex)
            {
                throw new Exception(ApplicationConstants.ErrorMessages.FieldEmpty);
            }

            //Validate if notes passed in are musical notes.            
            foreach (PropertyInfo propertyInfo in result.GetType().GetProperties())
            {
                if (propertyInfo.Name.Contains(ApplicationConstants.NoteFieldName))
                {
                    var fieldValue = result.GetType().GetProperty(propertyInfo.Name).GetValue(result, null);
                    if (!ApplicationConstants.NotesInScale.Contains(fieldValue.ToString().ToUpper()))
                        throw new Exception($@"{ApplicationConstants.ErrorMessages.InvalidNote} for {propertyInfo.Name}");
                }
            }

            return result;
        }

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

        //Get Chord by ChordName

        //Update Chord - assumes all fields are required. For updating 

        //Delete Chord

    }
}
