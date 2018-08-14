using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace WordBankServer
{
	class MainClass
	{
		private static Random randGen = new Random ();
		private static string homeDirectory = "./";
		private static string fileDirectory = homeDirectory + "WebServerFiles/";

		private static string wordList = fileDirectory + "wordList_8_13.txt";
		private static string templateFile = fileDirectory + "template.html";

		public static void Main (string[] args)
		{
			randGen = new Random (12345); // For now constant seed.

			// read in deck, parse, create cards, save
			ConceptDeck deck = new ConceptDeck(WordParser.ParseWords (randGen, wordList), randGen);

			Console.WriteLine(deck.ToString ());
			// Initialize template file
			ResponseUtils.InitializeTemplate(templateFile);

			// set up listeners
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add ("http://localhost:5432/");
            listener.Prefixes.Add("http://+:80/");
            listener.Start ();
			while (true) {
				HttpListenerContext context = listener.GetContext ();

				HttpListenerRequest request = context.Request;
				HttpListenerResponse response = context.Response;

				try {
					NameValueCollection queryParams = ResponseUtils.ParseQueryString (request.Url.Query);
					// Standard action should be "return the file at that spot"
					if (queryParams.Count == 0 && !request.Url.LocalPath.EndsWith("/"))
					{
						// return whatever file is asked for, if it's present.
						ResponseUtils.SendFileResponse(fileDirectory + request.Url.LocalPath, response);
						continue;
					}

					if (queryParams.Count == 0 && (request.Url.LocalPath.Equals ("") || request.Url.LocalPath.Equals("/")))
					{
						// Default draw behavior
						ConceptCard card = deck.DrawCard();
						ResponseUtils.SendCardResponse(response, card.Words);
						continue;
					}

					// ?action=draw
					if (request.Url.LocalPath.Equals("/play") && queryParams["action"] == "draw")
					{
						ResponseUtils.SendTextResponse(response, request.Url.Query);
						continue;
					}

					if (request.Url.LocalPath.Equals("/play") && queryParams["action"] == "discard")
					{
						ResponseUtils.SendTextResponse(response, request.Url.Query);
						continue;
					}
				}
				catch(Exception e) {
					Console.WriteLine (e);
//					response.StatusCode = (int) HttpStatusCode.BadRequest;
//					ResponseUtils.SendTextResponse (response, "Bad Request:" + e.Message);
				}

			}
			Console.WriteLine ("Hello World!");

		}


		public static void TestCardShuffling()
		{
			List<ConceptCard> parsedCards = new List<ConceptCard>();
			for (int i = 0; i < 100; i++) {
				parsedCards.Add (CreateTestCard (i));
			}

			ConceptDeck deck = new ConceptDeck (parsedCards, randGen);
			for (int i = 0; i < 101; i++) {
				ConceptCard drawn = deck.DrawCard ();
				deck.DiscardCard (drawn.id);
			}

			Console.WriteLine(deck.ToString ());
		}

		public static ConceptCard CreateTestCard(int id)
		{
			string[] words = { "test_" + id };
			return new ConceptCard (words, id);
		}
	}
}
