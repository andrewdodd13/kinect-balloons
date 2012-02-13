using Microsoft.Xna.Framework;

namespace BubblesClient.Input.Controllers
{
    /// <summary>
    /// IInputController defines the possible actions that can be performed in
    /// the game. Each Input Method retains a reference to this class, and 
    /// should call the appropriate method when it occurs.
    /// </summary>
    public interface IInputController
    {
        /// <summary>
        /// Initializes the Input Controller with the given screen dimensions.
        /// These should be stored by implementations as the other methods 
        /// should return positions relative to the screen.
        /// </summary>
        /// <param name="screenSize">A 2D vector of the screen size</param>
        void Initialize(Vector2 screenSize);

        /// <summary>
        /// Returns an array of Hands representing all the hands currently in
        /// focus for this input method. The Hand object should be kept between
        /// calls for the same physical hand; this allows the physics engine to
        /// correctly respond to movement by using spring forces rather than
        /// direct manipulation.
        /// </summary>
        /// <returns>An array of hands. Position should be relative to the 
        /// screen, as described by Initialize().</returns>
        Hand[] GetHandPositions();
    }
}
