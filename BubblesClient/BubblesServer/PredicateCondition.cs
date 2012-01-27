using System;
using System.Threading;

namespace BubblesServer
{
    /// <summary>
    /// Représente une condition utilisant un délégué pour vérifier si elle est vérifiée ou non.
    /// </summary>
    public class PredicateCondition
    {
        #region Champs
        readonly ConditionPredicate predicate;
        readonly object conditionLock;
        #endregion
        #region Constructeur
        /// <summary>
        /// Crée une nouvelle condition avec le délégué spécifié, qui utilise un objet interne pour la synchronisation.
        /// </summary>
        /// <param name="predicate"> Délégué à utiliser pour vérifier la condition. </param>        
        /// <remarks>
        /// Le verrou sur l'objet de synchronisation interne est acquis pendant l'appel du délégué.
        /// </remarks>
        public PredicateCondition( ConditionPredicate predicate ) : this( predicate, new object() )
        {
        }
        /// <summary>
        /// Crée une nouvelle condition avec le délégué spécifié et l'objet de synchronisation spécifiés.
        /// </summary>
        /// <param name="predicate"> Délégué à utiliser pour vérifier la condition. </param>
        /// <param name="conditionLock"> Objet à utiliser pour synchroniser l'accès à la condition. </param>
        /// <remarks>
        /// Le verrou sur <paramref name="conditionLock"/> est acquis pendant l'appel du délégué.
        /// </remarks>
        public PredicateCondition( ConditionPredicate predicate, object conditionLock )
        {
            if( predicate == null )
            {
                throw new ArgumentNullException( "predicate" );
            }
            else if( conditionLock == null )
            {
                throw new ArgumentNullException( "conditionLock" );
            }
            this.predicate = predicate;
            this.conditionLock = conditionLock;
        }
        #endregion
        #region Implémentation
        /// <summary>
        /// Attend que la condition soit vérifiée.
        /// </summary>
        public void Wait()
        {
            lock( conditionLock )
            {
                while( !predicate() )
                {
                    Monitor.Wait( conditionLock );
                }
            }
        }
        /// <summary>
        /// Signale que la valeur de la condition peut avoir changé, si c'est le cas, réveille un seul thread attendant que la condition soit vérifiée.
        /// </summary>
        public void Signal()
        {
            lock( conditionLock )
            {
                if( predicate() )
                {
                    Monitor.Pulse( conditionLock );
                }
            }
        }
        /// <summary>
        /// Signale que la valeur de la condition peut avoir changé, si c'est le cas, réveille tous les threads attendant que la condition soit vérifiée.
        /// </summary>
        public void SignalAll()
        {
            lock( conditionLock )
            {
                if( predicate() )
                {
                    Monitor.PulseAll( conditionLock );
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Délégué indiquant si une condition est vérifiée ou non.
    /// </summary>
    /// <returns> true si la condition est actuellement vérifiée, false sinon. </returns>
    public delegate bool ConditionPredicate();
}
