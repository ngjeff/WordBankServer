﻿using System;
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

		private static string wordList = fileDirectory + "wordList_8_17.txt";
		private static string templateFile = fileDirectory + "template.html";

        // set up some Regexes for later use by all threads
        private static Regex numberMatch = new Regex("[0-9]+");
        private static Regex customCardMatch = new Regex("(?i:CustomCard)_[0-9]+.jpg");

        // Save some variables for global use
        private static ConceptDeck deck;

        public static void Main (string[] args)
		{
            //// randGen = new Random (12345); // For now constant seed.
            randGen = new Random (23456); // For now constant seed.

            // Load up the blank card image.
            Image blankCard = Image.FromFile(fileDirectory + "blankCard.jpg");

			// read in deck, parse, create cards, save
			deck = new ConceptDeck(WordParser.ParseWords (randGen, wordList, blankCard), randGen);

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
                ThreadPool.QueueUserWorkItem(ProcessRequest, context);
			}
		}

        private static void ProcessRequest(object contextArg)
        {
            HttpListenerContext context = contextArg as HttpListenerContext;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                NameValueCollection queryParams = ResponseUtils.ParseQueryString(request.Url.Query);
                // Standard action should be "return the file at that spot"
                if (queryParams.Count == 0 && !request.Url.LocalPath.EndsWith("/"))
                {
                    if (customCardMatch.IsMatch(request.Url.LocalPath))
                    {
                        // the card images are served from memory.  Isolate the card number and pass along the card.
                        MatchCollection matches = numberMatch.Matches(request.Url.LocalPath);
                        ResponseUtils.SendCardGraphicResponse(response, deck.GetCardById(Int32.Parse(matches[0].Value)));
                    }
                    else
                    {
                        // return whatever file is asked for, if it's present.
                        ResponseUtils.SendFileResponse(fileDirectory + request.Url.LocalPath, response);
                    }
                }

                if (queryParams.Count == 0 && (request.Url.LocalPath.Equals("") || request.Url.LocalPath.Equals("/")))
                {
                    // Default draw behavior
                    int currentCard = 0;
                    ConceptCard card;
                    if (context.Request.Cookies != null &&
                        context.Request.Cookies["CardId"] != null &&
                        Int32.TryParse(context.Request.Cookies["cardid"].Value, out currentCard))
                    {
                        // If the user already has a card, and it's not expired,
                        // display to them the one they have.
                        card = deck.GetCardById(currentCard);
                        if (!card.IsExpired())
                        {
                            card.RefreshExpiry();
                            ResponseUtils.SendTemplateResponse(response, card);
                            return;
                        }
                    }

                    // Otherwise, it's first use--draw a card.
                    card = deck.DrawCard();
                    ResponseUtils.SendTemplateResponse(response, card);
                }

                // ?action=draw
                if (request.Url.LocalPath.Equals("/play") && queryParams["action"] == "draw")
                {
                    int currentCard = 0;
                    if (context.Request.Cookies != null &&
                        context.Request.Cookies["CardId"] != null &&
                        Int32.TryParse(context.Request.Cookies["cardid"].Value, out currentCard))
                    {
                        deck.DiscardCard(currentCard);
                    }

                    ConceptCard card = deck.DrawCard();
                    ResponseUtils.SendTemplateResponse(response, card);
                }

                // ?action=discard
                if (request.Url.LocalPath.Equals("/play") && queryParams["action"] == "discard")
                {
                    int currentCard = 0;
                    if (Int32.TryParse(context.Request.Cookies["cardid"].Value, out currentCard))
                    {
                        deck.DiscardCard(currentCard);
                    }

                    ResponseUtils.SendTemplateResponse(response, "No card present.  Select draw to draw a card.");
                }

                // ?action=refresh
                if (request.Url.LocalPath.Equals("/play") && queryParams["action"] == "refresh")
                {
                    int currentCard = 0;
                    if (Int32.TryParse(context.Request.Cookies["cardid"].Value, out currentCard))
                    {
                        ConceptCard card = deck.GetCardById(currentCard);
                        if (!card.IsExpired())
                        {
                            card.RefreshExpiry();
                        }
                    }

                    ResponseUtils.SendTextResponse(response, "Refresh");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //					response.StatusCode = (int) HttpStatusCode.BadRequest;
                //					ResponseUtils.SendTextResponse (response, "Bad Request:" + e.Message);
            }
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
