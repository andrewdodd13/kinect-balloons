namespace BubblesClient.Model.Buckets
{
    using Balloons.Messaging.Model;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Decoration Bucket is a Bucket which applies an overlay type to a 
    /// balloon. If the balloon already has this overlay then it removes the
    /// overlay.
    /// </summary>
    public class DecorationBucket : Bucket
    {
        public OverlayType OverlayType { get; set; }

        public DecorationBucket(Texture2D texture, OverlayType overlayType) :
            base(texture)
        {
            this.OverlayType = overlayType;
        }

        public override void ApplyToBalloon(ClientBalloon balloon)
        {
            if (balloon.OverlayType == this.OverlayType)
            {
                balloon.OverlayType = OverlayType.White;
            }
            else
            {
                balloon.OverlayType = this.OverlayType;
            }
        }
    }
}
