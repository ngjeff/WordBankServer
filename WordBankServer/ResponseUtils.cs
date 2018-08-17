using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace WordBankServer
{
	public class ResponseUtils
	{
		private static string templateFile;

		public static void InitializeTemplate(string templatePath)
		{
			templateFile = File.ReadAllText (templatePath);
		}

		public static void SendFileResponse(string path, HttpListenerResponse response) {
            using ( FileStream fs = File.OpenRead( path ) ) {
				response.ContentLength64 = fs.Length;
				response.SendChunked = false;
				if (path.EndsWith (".jpg")) {
					response.ContentType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
				} else if (path.EndsWith (".gif")) {
					response.ContentType = System.Net.Mime.MediaTypeNames.Image.Gif;
				}

                response.StatusCode = ( int )HttpStatusCode.OK;
				response.StatusDescription = "OK";

				byte[] buffer = new byte[ 64 * 1024 ];
				int read;
				using( BinaryWriter bw = new BinaryWriter( response.OutputStream ) ) {
					while( ( read = fs.Read( buffer, 0, buffer.Length ) ) > 0 ) {
						bw.Write( buffer, 0, read );
						bw.Flush(); //seems to have no effect
					}
				}

				response.OutputStream.Close();
			}
		}

		public static void SendTemplateResponse(HttpListenerResponse response, ConceptCard card)
		{
            // Looks like we need to write the card info in the template instead, and 
            // provide a way for the later call to get access to it.
            response.AppendCookie(new Cookie("cardid", card.id.ToString()));
			string returnFile = templateFile;
            if (card != null)
            {
                returnFile = SetRefresh(true, returnFile);
                returnFile = returnFile.Replace("__TEMPLATE_ITEM_1__", $"<IMG SRC = \"CustomCard_{card.id}.jpg\" ID=\"bg\">");
            }
            else
            {
                returnFile = SetRefresh(false, returnFile);
                returnFile = returnFile.Replace("__TEMPLATE_ITEM_1__", "The draw pile is empty.  Wait for another player to discard a card!");
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(returnFile);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
			// You must close the output stream.
			output.Close();
        }

        private static string SetRefresh(bool refresh, string returnTemplate)
        {
            if (refresh)
            {
                return returnTemplate.Replace("__TEMPLATE_ITEM_0__", "setInterval(RefreshExpiry, 30000);");
            }
            else
            {
                return returnTemplate.Replace("__TEMPLATE_ITEM_0__", "");

            }
        }

        public static void SendTemplateResponse(HttpListenerResponse response, string text)
        {
            // Use this to provide text to the user, while still showing the buttons for action.
            Cookie deletedCookie = new Cookie("cardid", "NO_CARD");
            deletedCookie.Expires = DateTime.MinValue;
            response.AppendCookie(deletedCookie);

            string returnFile = templateFile;
            returnFile = SetRefresh(false, returnFile);
            returnFile = returnFile.Replace("__TEMPLATE_ITEM_1__", $"<DIV class=\"infotext\">{text}</DIV>");

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(returnFile);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }


        public static void SendCardGraphicResponse(HttpListenerResponse response, ConceptCard card)
        {
            response.AddHeader("Cache-Control", "no-transform, public, max-age=900");
            response.AddHeader("Etag", card.GetHashCode().ToString());

            // Pull the image out of the card in memory and send it down.
            response.ContentLength64 = card.CardImage.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(card.CardImage, 0, card.CardImage.Length);
            // You must close the output stream.
            output.Close();
        }

            public static void SendTextResponse(HttpListenerResponse response, string outputValue)
		{
			// Construct a response.
			string responseString = "<HTML><BODY>" + outputValue + "</BODY></HTML>";
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
			// You must close the output stream.
			output.Close();
		}

		// Parse a query string's arguments. The Query comes in as ?a=b&c=d
		public static NameValueCollection ParseQueryString(string query)
		{
			NameValueCollection collection = new NameValueCollection ();
			if (!query.StartsWith ("?")) {
				return collection;
			}

			string args = query.Substring (1); // trim the ?
			string[] splitArgs = args.Split('&');
			foreach (string valuePair in splitArgs) {
				string[] splitPair = valuePair.Split ('=');
				collection [splitPair [0]] = splitPair [1];
			}

			return collection;
		}
	}
}

