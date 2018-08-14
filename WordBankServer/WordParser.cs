using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordBankServer
{
	public class WordParser
	{
		public static List<ConceptCard> ParseWords (Random randGen, string filePath)
		{
			// read all the words, and put them in sorted by difficulty.
			string[] lines = System.IO.File.ReadAllLines(filePath);

			// insert into three lists.
			List<string> easyWords = new List<string>();
			List<string> mediumWords = new List<string>();
			List<string> hardWords = new List<string>();

			foreach (string line in lines) {
				string[] splitLine = line.Split('\t');
				switch (splitLine [1]) {
				case "1":
					easyWords.Add (splitLine [0]);
					break;
				case "2":
					mediumWords.Add (splitLine [0]);
					break;
				case "3":
					hardWords.Add (splitLine [0]);
					break;
				default:
					throw new FormatException ("Invalid difficulty in line " + line);
				}
			}

			List<ConceptCard> results = new List<ConceptCard> ();
			int cardNum = 0;
			// read in three words from each difficulty to make a card, until we're < 3 each.
			while (easyWords.Count > 3 && mediumWords.Count > 3 && hardWords.Count > 3) {
				List<string> wordsForCard = new List<string> ();
				for (int i = 0; i < 3; i++) {
					wordsForCard.Add(RemoveAndRetrieveWord (easyWords, randGen));
				}
				for (int i = 0; i < 3; i++) {
					wordsForCard.Add(RemoveAndRetrieveWord (mediumWords, randGen));
				}
				for (int i = 0; i < 3; i++) {
					wordsForCard.Add(RemoveAndRetrieveWord (hardWords, randGen));
				}

				results.Add (new ConceptCard (wordsForCard.ToArray(), cardNum));
				cardNum++;
			}

			return results;
		}

		private static string RemoveAndRetrieveWord(List<string> words, Random randGen)
		{
			int index = randGen.Next (0, words.Count);
			string word = words [index];
			words.RemoveAt (index);
			return word;
		}
	}
}

