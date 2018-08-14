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
			using( FileStream fs = File.OpenRead( fileDirectory + path ) ) {

				//response is HttpListenerContext.Response...
				response.ContentLength64 = fs.Length;
				response.SendChunked = false;
				if (path.EndsWith (".jpg")) {
					response.ContentType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
				} else if (path.EndsWith (".gif")) {
					response.ContentType = System.Net.Mime.MediaTypeNames.Image.Gif;
				}

				byte[] buffer = new byte[ 64 * 1024 ];
				int read;
				using( BinaryWriter bw = new BinaryWriter( response.OutputStream ) ) {
					while( ( read = fs.Read( buffer, 0, buffer.Length ) ) > 0 ) {
						bw.Write( buffer, 0, read );
						bw.Flush(); //seems to have no effect
					}
				}

				response.StatusCode = ( int )HttpStatusCode.OK;
				response.StatusDescription = "OK";
				response.OutputStream.Close();
			}
		}

		public static void SendCardResponse(HttpListenerResponse response, string[] words)
		{
			string returnFile = templateFile;
			for (int i = 0; i < words.Length; i++) {
				returnFile.Replace ("__TEMPLATE_ITEM_" + (i + 1) + "__", words [i]);
			}

			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(returnFile);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
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

