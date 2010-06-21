// <copyright file="NotifoClient.cs" company="ramonsmits.com">
//
// Copyright (c) 2010 Ramon Smits (http://ramonsmits.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// </copyright>
namespace Exyll.Notifications
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web;

    /// <summary>
    /// A simple client to send notifications via Notifo.
    /// </summary>
    /// <remarks>
    /// This code has a dependancy on the .NET v4.x client profile framework.
    /// See Notifo API documentation at: https://api.notifo.com/
    /// </remarks>
    /// <example>
    /// var notifo = new NotifoClient(username, secret);
    ///
    /// notifo.SendNotification(
    ///     username,
    ///     "Testing 1, 2, 3 @ " + DateTime.Now,
    ///     "Meet the creator",
    ///     "http://maps.google.com/maps?q=Rotterdam,Netherlands",
    ///     "Demo");
    /// </example>
    public class NotifoClient
    {
        const string BaseUrl = "https://api.notifo.com/v1/";
        const string SendNotificationUrl = BaseUrl + "send_notification";
        const string SubscribeUserUrl = BaseUrl + "subscribe_user";
        readonly DataContractJsonSerializer _notifoResultSerializer = new DataContractJsonSerializer(typeof(send_notification_result));
        readonly string _authorizationHeaderValue;

        [Serializable]
        internal class send_notification_result
        {
            public string status;           //INFO: Ignore compiler warning. Assigned by deserialisation of result messages
            public int response_code;       //INFO: Ignore compiler warning. Assigned by deserialisation of result messages
            public string response_message; //INFO: Ignore compiler warning. Assigned by deserialisation of result messages
        }

        /// <summary>
        /// Create a Notifo instance.
        /// </summary>
        /// <param name="username">Notifo username</param>
        /// <param name="secret">Notifo secret. See Notifo API page to retrieve your personal secret.</param>
        public NotifoClient(
            string username, 
            string secret)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            if (string.IsNullOrEmpty(secret)) throw new ArgumentNullException("secret");

            _authorizationHeaderValue = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + secret));
        }

        /// <summary>
        /// Send a nofication message and waits until the message has been transmitted to the notifo server.
        /// </summary>
        /// <param name="to">Account name to send the notification message to.</param>
        /// <param name="msg">The notification message to send</param>
        /// <param name="title">An optional title</param>
        /// <param name="uri">An optional uri that can refer to for example a details page.</param>
        /// <param name="label">An optional label to use. Works only for personal notifcations as stated by the Notifo API documentation.</param>
        public void SendNotification(
            string to, 
            string msg, 
            string title, 
            string uri, 
            string label)
        {
            if (string.IsNullOrEmpty(to)) throw new ArgumentNullException("to");
            if (string.IsNullOrEmpty(msg)) throw new ArgumentNullException("msg");

            var request = PrepareRequest(SendNotificationUrl);

            var fields = new Dictionary<string, string>()
            {
                { "to", to },
                { "msg", msg },
                { "title", title },
                { "uri", uri },
                { "label", label },
            };

            WriteValues(request, fields);

            var response = request.GetResponse();

            if (!ProcessResponse(response))
                throw new Exception("Send notification failed.");
        }

        /// <summary>
        /// Send a subscribe user message and waits until the message has been transmitted to the notifo server.
        /// </summary>
        /// <param name="username">The notifo user account to subscribe.</param>
        public void SubscribeUser(string username)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");

            var request = PrepareRequest(SubscribeUserUrl);

            var fields = new Dictionary<string, string>()
            {
                { "username", username }
            };

            WriteValues(request, fields);

            var response = request.GetResponse();

            if (!ProcessResponse(response))
                throw new Exception("Subscribe user failed.");
        }

        /// <summary>
        /// Makes sure that the value that is supplied can be user on an URL.
        /// </summary>
        /// <param name="value">The non escaped string value</param>
        /// <returns>Escaped string value to be used in a url</returns>
        string Escape(string @value)
        {
            return Uri.EscapeDataString(@value);
        }

        /// <summary>
        /// Initializes a http request to be send to the notifo server.
        /// </summary>
        /// <param name="url">The url to post to.</param>
        /// <returns>An initialized http web request.</returns>
        HttpWebRequest PrepareRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("Authorization", _authorizationHeaderValue);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json, text/json";
            return request;
        }

        /// <summary>
        /// Processes the web response to check if the message has been transmitted succesfully.
        /// </summary>
        /// <param name="response">A web response returned from the notifo api webserver.</param>
        /// <returns>True if everything is ok.</returns>
        bool ProcessResponse(WebResponse response)
        {
            using (var responseStream = response.GetResponseStream())
            {
                var result = (send_notification_result)_notifoResultSerializer.ReadObject(responseStream);
                return result.response_code == 2201 || result.response_code == 2202;
            }
        }

        /// <summary>
        /// Writes the given key/value items to the web request.
        /// </summary>
        /// <param name="request">Web request to use</param>
        /// <param name="values">Key/value items to write</param>
        void WriteValues(WebRequest request, IDictionary<string, string> values)
        {
            using (var requestStream = request.GetRequestStream())
            using (var writer = new StreamWriter(requestStream))
            {
                foreach (var v in values)
                {
                    writer.Write(v.Key);
                    writer.Write("=");
                    writer.Write(Escape(v.Value));
                    writer.Write("&");
                }
            }
        }
    }
}
