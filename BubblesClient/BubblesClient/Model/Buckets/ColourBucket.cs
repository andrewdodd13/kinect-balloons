namespace BubblesClient.Model.Buckets
{
    using Balloons.Messaging.Model;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Colour Bucket applies a coloured paint to the balloon. The colour of the
    /// paint in this bucket is defined by the Colour property; colour 
    /// combinatorics are defined arbitrarily in the Apply method. At the moment
    /// only red/green/blue are supported unfortunately.
    /// </summary>
    public class ColourBucket : Bucket
    {
        public Color Colour { get; set; }

        public ColourBucket(Texture2D texture, Color colour) :
            base(texture)
        {
            this.Colour = colour;
        }

        public override void ApplyToBalloon(ClientBalloon balloon)
        {
            int count = 0;
            if (balloon.BackgroundColor.Blue == 0) count++;
            if (balloon.BackgroundColor.Red == 0) count++;
            if (balloon.BackgroundColor.Green == 0) count++;

            if (this.Colour == Color.Red)
            {
                switch (count)
                {
                    case 0:
                        balloon.BackgroundColor = new Colour(255, 0, 0, 255);
                        break;
                    case 1:
                        if (balloon.BackgroundColor.Red == 128)
                            balloon.BackgroundColor = new Colour(255, 0, 0, 255);
                        else
                            balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                        break;
                    case 2:
                        if (balloon.BackgroundColor.Red == 0 && balloon.BackgroundColor.Blue == 255)
                            balloon.BackgroundColor = new Colour(128, 0, 128, 255);
                        else if (balloon.BackgroundColor.Red == 0 && balloon.BackgroundColor.Green == 255)
                            balloon.BackgroundColor = new Colour(128, 128, 0, 255);
                        break;
                }
            }
            else if (this.Colour == Color.Green)
            {
                switch (count)
                {
                    case 0:
                        balloon.BackgroundColor = new Colour(0, 255, 0, 255);
                        break;
                    case 1:
                        if (balloon.BackgroundColor.Green == 128)
                            balloon.BackgroundColor = new Colour(0, 255, 0, 255);
                        else
                            balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                        break;
                    case 2:
                        if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Blue == 255)
                            balloon.BackgroundColor = new Colour(0, 128, 128, 255);
                        else if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Red == 255)
                            balloon.BackgroundColor = new Colour(128, 128, 0, 255);
                        break;
                }
            }
            else if (this.Colour == Color.Blue)
            {
                switch (count)
                {
                    case 0:
                        balloon.BackgroundColor = new Colour(0, 0, 255, 255);
                        break;
                    case 1:
                        if (balloon.BackgroundColor.Blue == 128)
                            balloon.BackgroundColor = new Colour(0, 0, 255, 255);
                        else
                            balloon.BackgroundColor = new Colour(128, 128, 128, 255);
                        break;
                    case 2:
                        if (balloon.BackgroundColor.Blue == 0 && balloon.BackgroundColor.Green == 255)
                            balloon.BackgroundColor = new Colour(0, 128, 128, 255);
                        else if (balloon.BackgroundColor.Green == 0 && balloon.BackgroundColor.Red == 255)
                            balloon.BackgroundColor = new Colour(128, 0, 128, 255);
                        break;
                }
            }
        }
    }
}
