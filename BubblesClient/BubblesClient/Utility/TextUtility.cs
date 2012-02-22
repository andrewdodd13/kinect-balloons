namespace BubblesClient.Utility
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Contains utilities for manipulating text.
    /// </summary>
    public class TextUtility
    {
        public static void drawTextLabel(SpriteBatch spriteBatch, SpriteFont font, String text, Vector2 pos)
        {
            drawTextLabel(spriteBatch, font, text, pos, Color.Black);
        }

        public static void drawTextLabel(SpriteBatch spriteBatch, SpriteFont font, String text, Vector2 pos, Color color)
        {
            try
            {
                spriteBatch.DrawString(font, text, pos, color);
            }
            catch (Exception)
            {
                spriteBatch.DrawString(font, "Invalid character", pos, Color.Red);
            }
        }

        /// <summary>
        /// This function takes a string and a vector2. It splits the string given by spaces
        /// and then for each word it will check the length of it against the length of the
        /// vector2 given in. When the word is passed the edge a new line is put in.
        /// </summary>
        /// <param name="font">The font to use for measuments</param>
        /// <param name="text">Text to be wrapped</param>
        /// <param name="containerDemensions">Dimensions of the container the text is to be wrapped in</param>
        /// <returns>The text including newlines to fit into the container</returns>
        public static String wrapText(SpriteFont font, String text, Vector2 containerDemensions)
        {
            String line = String.Empty;
            String returnString = String.Empty;
            String[] wordArray = text.Split(' ');

            foreach (String word in wordArray)
            {
                if (font.MeasureString(line + word).Length() > containerDemensions.X)
                {
                    returnString += line + '\n';
                    line = String.Empty;

                    // If the string is longer than the box, we need to stop
                    if (font.MeasureString(returnString).Y > containerDemensions.Y)
                    {
                        returnString = returnString.Substring(0, returnString.Length - 3) + "...";
                        break;
                    }
                }

                line += word + ' ';
            }

            return returnString + line;
        }
    }
}
