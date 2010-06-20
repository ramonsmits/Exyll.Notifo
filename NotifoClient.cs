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

            var request = (HttpWebRequest)WebRequest.Create(SendNotificationUrl);
            request.Method = "POST";
            request.Headers.Add("Authorization", _authorizationHeaderValue);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Accept = "application/json, text/json";

            var requestStream = request.GetRequestStream();

            using (var requestStreamWriter = new StreamWriter(requestStream))
            {
                requestStreamWriter.Write(
                    @"to={0}&msg={1}&title={2}&uri={3}&label={4}",
                    Escape(to),     // 0
                    Escape(msg),    // 1
                    Escape(title),  // 2
                    Escape(uri),    // 3
                    Escape(label)   // 4
                    );
            }

            var response = request.GetResponse();

            using (var responseStream = response.GetResponseStream())
            {
                var result = (send_notification_result)_notifoResultSerializer.ReadObject(responseStream);
                if (result.response_code != 2201)
                {
                    throw new Exception("Send notification failed. Reason: " + result.response_code);
                }
            }
        }

        string Escape(string @value)
        {
            return Uri.EscapeDataString(@value);
        }
    }
}
