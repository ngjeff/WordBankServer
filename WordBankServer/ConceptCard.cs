using System;

namespace WordBankServer
{
	public class ConceptCard
	{
		private string[] words;
		private DateTime leaseExpiry; // Card is out to a client for a certain amount of time.
								      // Auto-discarded if out past expiry.
		public int id { get; private set; }  // Clients use this to refer to the card


		public ConceptCard (string[] words, int id)
		{
			// Contains the words
			this.words = words;
			this.id = id;

			// Contains art template
		}

		public string Render()
		{
			// Contains a way to render in HTML.  Could be pieces of the image cut up.
			return string.Join(",", words);
		}

		public override string ToString()
		{
			return "id:" + id + "leaseExpiry:" + leaseExpiry + "words:" + string.Join (",", words);
		}
	}
}

