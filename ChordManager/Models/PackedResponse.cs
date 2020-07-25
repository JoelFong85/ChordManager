using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

    }
}