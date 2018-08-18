using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace WordBankServer
{
	class MainClass
	{
		private static Random randGen = new Random ();
		private static string homeDirectory = "./";
		private static string fileDirectory = homeDirectory + "WebServerFiles/";

		private static string xboxWordList = fileDirectory + "wordList_8_17.txt";
        private static string picnicWordList = fileDirectory + "WordBankPicnic.txt";
        private static string templateFile = fileDirectory + "template.html";

        // set up some Regexes for later use by all threads
        private static Regex numberMatch = new Regex("[0-9]+");
        private static Regex customCardMatch = new Regex("(?i:CustomCard)_[0-9]+.jpg");

        // Save some variables for global use
        private static Dictionary<string, ConceptDeck> decks = new Dictionary<string, ConceptDeck>();
        private const string XboxDeckName = "xbox";
        private const string PicnicDeckName = "picnic";

        public static void Main (string[] args)
		{
            //// randGen = new Random (12345); // For now constant seed.
            randGen = new Random (56789); // For now constant seed.

            // Load up the blank card image.
            Image blankCard = Image.FromFile(fileDirectory + "blankCard.jpg");

			// read in deck, parse, create cards, save
			decks[XboxDeckName] = new ConceptDeck(WordParser.ParseWords (randGen, xboxWordList, blankCard), randGen);
            decks[PicnicDeckName] = new ConceptDeck(WordParser.ParseWords(randGen, picnicWordList, blankCard), randGen);

            Console.WriteLine(decks[XboxDeckName].ToString ());
			
            // Initialize template file
			ResponseUtils.InitializeTemplate(templateFile);
            
            // set up listeners
            HttpListener listener = new HttpListener();
			listener.Prefixes.Add ("http://localhost:5432/");
            listener.Prefixes.Add("http://+:80/");
            listener.Start ();
			while (true) {
				HttpListenerContext context = listener.GetContext ();
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
			}
		}

        private static void ProcessRequest(object contextArg)
        {
            HttpListenerContext context = contextArg as HttpListenerContext;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // read the first part of the query string.  If it's the name of a deck, then find the appopriate deck for use.
            // should be exactly two or three segments in the URI.

            // Valid items:
            //  / xbox/
            //  / xbox/ [filename]
            //  / xbox/play?action=draw
            //  / xbox/play?action=discard
              
            if ((request.Url.Segments.Length < 2) || (request.Url.Segments.Length > 3))
            {
                // 404 it.
                ResponseUtils.SendTextResponse(response, "Invalid URL.");
                return;
            }

            // read the deck name segment and trim the forward slash.
            string deckName = request.Url.Segments[1];
            deckName = deckName.Substring(0, deckName.Length - 1);

            if (!decks.ContainsKey(deckName))
            {
                // 404 it.
                ResponseUtils.SendTextResponse(response, "Invalid deck name.");
                return;
            }

            // ConceptDeck deck = decks[request.Url.Segments[1]];
            ConceptDeck deck = decks[deckName];
            string cookieName = $"{deckName}_cardid";
            try
            {
                NameValueCollection queryParams = ResponseUtils.ParseQueryString(request.Url.Query);
                // If it doesn't end in a /, it indicates a file.
                if (queryParams.Count == 0 && !request.Url.LocalPath.EndsWith("/"))
                {
                    if (customCardMatch.IsMatch(request.Url.LocalPath))
                    {
                        // the card images are served from memory.  Isolate the card number and pass along the card.
                        MatchCollection matches = numberMatch.Matches(request.Url.Segments[2]);
                        ResponseUtils.SendCardGraphicResponse(response, deck.GetCardById(Int32.Parse(matches[0].Value)));
                    }
                    else
                    {
                        // return whatever file is asked for, if it's present.
                        ResponseUtils.SendFileResponse(fileDirectory + request.Url.Segments[2], response);
                    }
                    return;
                }

                // If it does end in a /, it's the root of a deck.  Perform a default first behavior.
                if (queryParams.Count == 0 && (request.Url.LocalPath.Equals("") || request.Url.LocalPath.EndsWith("/")))
                {
                    int currentCard = 0;
                    ConceptCard card;
                    if (context.Request.Cookies != null &&
                        context.Request.Cookies[cookieName] != null &&
                        Int32.TryParse(context.Request.Cookies[cookieName].Value, out currentCard))
                    {
                        // If the user already has a card, and it's not expired,
                        // display to them the one they have.
                        card = deck.GetCardById(currentCard);
                        if (!card.IsExpired())
                        {
                            card.RefreshExpiry();
                            ResponseUtils.SendTemplateResponse(response, cookieName, card);
                            return;
                        }
                    }

                    // Otherwise, it's first use--draw a card.
                    card = deck.DrawCard();
                    ResponseUtils.SendTemplateResponse(response, cookieName, card);
                    return;
                }

                // ?action=draw
                if (request.Url.LocalPath.EndsWith("/play") && queryParams["action"] == "draw")
                {
                    int currentCard = 0;
                    if (context.Request.Cookies != null &&
                        context.Request.Cookies[cookieName] != null &&
                        Int32.TryParse(context.Request.Cookies[cookieName].Value, out currentCard))
                    {
                        deck.DiscardCard(currentCard);
                    }

                    ConceptCard card = deck.DrawCard();
                    ResponseUtils.SendTemplateResponse(response, cookieName, card);
                    return;
                }

                // ?action=discard
                if (request.Url.LocalPath.EndsWith("/play") && queryParams["action"] == "discard")
                {
                    int currentCard = 0;
                    if (context.Request.Cookies != null &&
                        context.Request.Cookies[cookieName] != null &&
                        Int32.TryParse(context.Request.Cookies[cookieName].Value, out currentCard))
                    {
                        deck.DiscardCard(currentCard);
                    }

                    ResponseUtils.SendTemplateResponse(response, cookieName, "No card present.  Select draw to draw a card.");
                    return;
                }

                // ?action=refresh
                if (request.Url.LocalPath.EndsWith("/play") && queryParams["action"] == "refresh")
                {
                    int currentCard = 0;
                    if (Int32.TryParse(context.Request.Cookies[cookieName].Value, out currentCard))
                    {
                        ConceptCard card = deck.GetCardById(currentCard);
                        if (!card.IsExpired())
                        {
                            card.RefreshExpiry();
                        }
                    }

                    ResponseUtils.SendTextResponse(response, "Refresh");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //					response.StatusCode = (int) HttpStatusCode.BadRequest;
                //					ResponseUtils.SendTextResponse (response, "Bad Request:" + e.Message);
            }

            ResponseUtils.SendTextResponse(response, "No matching handler!");
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
			return new ConceptCard (words, id, null);
		}
	}
}
