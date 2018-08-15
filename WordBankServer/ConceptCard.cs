using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WordBankServer
{
	public class ConceptCard
	{
		public string[] Words { get; private set; }
		private DateTime leaseExpiry; // Card is out to a client for a certain amount of time.
								      // Auto-discarded if out past expiry.
		public int id { get; private set; }  // Clients use this to refer to the card
        public int[] verticalOffsets = new int[] { 150, 230, 310, 480, 560, 640, 810, 890, 970 };

        public byte[] CardImage { get; private set; }

        public ConceptCard (string[] words, int id, Image cardTemplate)
		{
			// Contains the words
			this.Words = words;
			this.id = id;

            // Contains art template
            if (cardTemplate != null)
            {
                GenerateImage(cardTemplate);
            }
		}

		public void GenerateImage(Image template)
		{
            Image card = (Image)template.Clone();
            Graphics g = Graphics.FromImage(card);
            int verticalOffsetIndex = 0;
            foreach (string word in Words)
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                Font bestFitFont = FindBestFitFont(g, word, new Font("Arial", 20), new Size(500, 0));
                g.DrawString(word, bestFitFont, Brushes.Black,new Point((int) card.Width / 2, verticalOffsets[verticalOffsetIndex]), sf);
                verticalOffsetIndex++;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                Encoder myEncoder = Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 30L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                card.Save(ms, jpgEncoder, myEncoderParameters);
                CardImage = ms.ToArray();
            }
        }

        private Font FindBestFitFont(Graphics g, String text, Font font, Size proposedSize)
        {
            // Compute actual size, shrink if needed
            while (true)
            {
                SizeF size = g.MeasureString(text, font);

                // It fits, back out
                if (size.Width <= proposedSize.Width) { return font; }

                // Try a smaller font (90% of old size)
                Font oldFont = font;
                font = new Font(font.Name, (float)(font.Size * .9), font.Style);
                oldFont.Dispose();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public override string ToString()
		{
			return "id:" + id + "leaseExpiry:" + leaseExpiry + "words:" + string.Join (",", Words);
		}
	}
}

