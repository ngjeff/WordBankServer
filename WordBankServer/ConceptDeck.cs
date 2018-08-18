using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordBankServer
{
	public class ConceptDeck
	{
		private object deckLock = new object ();

		private Queue<ConceptCard> drawPile = new Queue<ConceptCard>();
		private List<ConceptCard> cardsInUse = new List<ConceptCard>();
		private List<ConceptCard> discardPile;

        private ConceptCard[] cardsById;

		private Random randGen;

		// Create a deck to draw from.
		public ConceptDeck (List<ConceptCard> initialPile, Random randGen)
		{
            this.randGen = randGen;

            cardsById = new ConceptCard[initialPile.Count];
            foreach (ConceptCard card in initialPile)
            {
                cardsById[card.id] = card;
            }

            lock (deckLock) {
				this.discardPile = initialPile;
				ShuffleDiscard ();
			}
		}

		public ConceptCard DrawCard()
		{
            ConceptCard drawnCard;
            lock (deckLock)
            {
                // Auto-discard any expired cards.
                List<ConceptCard> expiredCards = new List<ConceptCard>();
                foreach (ConceptCard card in cardsInUse)
                {
                    if (card.IsExpired())
                    {
                        expiredCards.Add(card);
                    }
                }

                foreach (ConceptCard card in expiredCards)
                {
                    Console.WriteLine($"{DateTime.Now}:Card {card.id} expired and auto-discaded.");
                    discardPile.Add(card);
                    cardsInUse.Remove(card);
                }

                if (drawPile.Count == 0 && discardPile.Count == 0)
                {
                    Console.WriteLine($"{DateTime.Now}:All cards are in use!  Cannot draw a card.");
                    return null;
                }

				if (drawPile.Count == 0) {
					ShuffleDiscard ();
				}
				drawnCard = drawPile.Dequeue ();
				cardsInUse.Add (drawnCard);
            }

            drawnCard.RefreshExpiry();
            Console.WriteLine($"{DateTime.Now}:Card {drawnCard.id} drawn.  {drawPile.Count} cards left in deck.");
			return drawnCard;
		}

		public void DiscardCard(int cardId)
		{
            if (cardsInUse.Where(p => p.id == cardId).Count() == 0)
            {
                Console.WriteLine($"{DateTime.Now}:Attempt to discard card {cardId} which is not in use.");
                return;
            }

            Console.WriteLine($"{DateTime.Now}:Card {cardId} discarded.");
            lock (deckLock)
            {
                ConceptCard chosenCard = cardsInUse.Where(p => p.id == cardId).First();
                discardPile.Add(chosenCard);
                cardsInUse.Remove(chosenCard);
            }
		}

		// This modifies both decks.  Must be called withn the deckLock.
		private void ShuffleDiscard()
		{
            Console.WriteLine($"{DateTime.Now}:Deck empty, reshuffling.");
            // pull cards out out of the discard in a random order, and insert into the draw pile.
            while (discardPile.Count != 0) {
				int index = randGen.Next (0, discardPile.Count - 1);
				ConceptCard card = discardPile [index];
				drawPile.Enqueue (card);
				discardPile.RemoveAt (index);
			}
		}

        public ConceptCard GetCardById(int id)
        {
            return cardsById[id];
        }

        public override string ToString()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ("DrawPile:");
			foreach (ConceptCard card in drawPile) {
				sb.AppendLine (card.ToString());
			}
			sb.AppendLine ("cardsInUse:");
			foreach (ConceptCard card in cardsInUse) {
				sb.AppendLine (card.ToString());
			}
			sb.AppendLine ("DiscardPile:");
			foreach (ConceptCard card in discardPile) {
				sb.AppendLine (card.ToString());
			}
			return sb.ToString ();
		}

	}
}

