using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using WebGrease;

namespace ChordManager.Models
{
    public class PackedResponse
    {
        private string ResponseMessage { get; set; }
        private HttpStatusCode HttpStatus { get; set; }

        public PackedResponse(HttpStatusCode httpStatus, string responseMessage)
        {
            HttpStatus = httpStatus;
            ResponseMessage = responseMessage;
        }

        public HttpResponseMessage Pack()
        {
            var response = new HttpResponseMessage(HttpStatus);
            response.Content = new StringContent(ResponseMessage);
            return response;
        }

        public HttpResponseMessage PackAsJson()
        {
            var response = new HttpResponseMessage(HttpStatus);
            response.Content = new StringContent(ResponseMessage, Encoding.UTF8, "application/json");
            return response;
        }

        public HttpResponseMessage PackAsDownload(MemoryStream dataStream, string chordName, string audioFileExt)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(dataStream);
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = $@"{chordName}.{audioFileExt}";
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return response;
        }

    }
}