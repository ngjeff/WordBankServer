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

		private Random randGen;

		// Create a deck to draw from.
		public ConceptDeck (List<ConceptCard> initialPile, Random randGen)
		{
			this.randGen = randGen;
			lock (deckLock) {
				this.discardPile = initialPile;
				ShuffleDiscard ();
			}
		}

		public ConceptCard DrawCard(int user)
		{
			lock (deckLock) {
				if (drawPile.Count == 0) {
					ShuffleDiscard ();
				}
				ConceptCard drawnCard = drawPile.Dequeue ();
				cardsInUse.Add (drawnCard);
				//TODO: Mark the card with user for... auth?  Or something?
				return drawnCard;
			}
		}

		public void DiscardCard(int cardId)
		{
			ConceptCard chosenCard = cardsInUse.Where (p => p.id == cardId).First ();
			discardPile.Add (chosenCard);
			cardsInUse.Remove (chosenCard);
		}

		// This modifies both decks.  Must be called withn the deckLock.
		private void ShuffleDiscard()
		{
			// pull cards out out of the discard in a random order, and insert into the draw pile.
			while (discardPile.Count != 0) {
				int index = randGen.Next (0, discardPile.Count - 1);
				ConceptCard card = discardPile [index];
				drawPile.Enqueue (card);
				discardPile.RemoveAt (index);
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ("DrawPile");
			foreach (ConceptCard card in drawPile) {
				sb.AppendLine (card.ToString());
			}
			sb.AppendLine ("cardsInUse");
			foreach (ConceptCard card in cardsInUse) {
				sb.AppendLine (card.ToString());
			}
			sb.AppendLine ("DiscardPile");
			foreach (ConceptCard card in discardPile) {
				sb.AppendLine (card.ToString());
			}
			return sb.ToString ();
		}

	}
}

