using System;
using System.Threading;

namespace BubblesServer
{
    /// <summary>
    /// Buffer circulaire utilisé pour synchroniser des données entre un producteur et un consommateur.
    /// Le producteur et le consommateur ne peuvent pas utiliser le buffer en même temps.
    /// </summary>
    public sealed class ExclusiveCircularQueue<T> : CircularQueue<T>
    {
		#region Champs
        //T[] buffer        :   synchronisé par syncExcl
        //int capacity      :   synchronisé par syncExcl
        //int available     :   synchronisé par syncExcl
        //int readPosition  :   synchronisé par syncExcl
        //int writePosition :   synchronisé par syncExcl
		readonly object syncExcl = new object();
        PredicateCondition canRead;
        PredicateCondition canWrite;
		#endregion
        #region Propriétés
        /// <summary>
        /// Obtient le nombre d'éléments dans le buffer.
        /// </summary>
        public override int Count
        {
            get
            {
                lock( syncExcl )
                {
                    return available;
                }
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être écrit dans le buffer.
        /// </summary>
        public override bool Full
        {
            get
            {
                lock( syncExcl )
                {
                    return (capacity == 0);
                }
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être lu dans le buffer.
        /// </summary>
        public override bool Empty
        {
            get
            {
                lock( syncExcl )
                {
                    return (available == 0);
                }
            }
        }
        #endregion
		#region Constructeur
        /// <summary>
        /// Crée un nouveau buffer circulaire avec la taille spécifiée.
        /// </summary>
        /// <param name="size"> Taille du buffer. </param>
        public ExclusiveCircularQueue( int size ) : base( size )
		{
            Initialize();
        }
        /// <summary>
        /// Crée un nouveau buffer circulaire à partir des éléments présents dans un tableau.
        /// </summary>
        /// <param name="array"> Tableau à partir duquel créer le buffer circulaire. </param>
        /// <param name="ownsArray"> Indique si le buffer utilise directement le tableau (true) ou copie son contenu dans un nouveau tableau (false). </param>
        /// <param name="empty"> Indique si le buffer doit être considéré comme vide ou plein à sa création. </param>
        /// <remarks> Si ownsArray vaut false, le tableau est copié dans le buffer en une opération rapide qui ne met pas en jeu de verrou. </remarks>
        public ExclusiveCircularQueue( T[] array, bool ownsArray, bool empty ) : base( array, ownsArray, empty )
		{
            Initialize();
        }
		#endregion
		#region Implémentation
        private void Initialize()
        {
            canRead = new PredicateCondition( delegate() { return available != 0; }, syncExcl );
            canWrite = new PredicateCondition( delegate() { return capacity != 0; }, syncExcl );
        }
        /// <summary>
        /// Ajoute une valeur à la fin du buffer circulaire.
        /// </summary>
        /// <param name="value"> Valeur à ajouter. </param>
        public override void Enqueue( T value )
        {
			lock( syncExcl )
			{
				//attend qu'il y ait de la place dans le buffer
                canWrite.Wait();

                --capacity;
                EnqueueCore( value );
				++available;

				//avertir les threads attendant pour la lecture
                canRead.SignalAll();                
			}
		}
        /// <summary>
        /// Retire la valeur au début du buffer circulaire.
        /// </summary>
        /// <returns> Valeur retirée. </returns>
        public override T Dequeue()
        {
			T temp;

			lock( syncExcl )
			{
				//attend qu'il y ait des valeurs à lire
                canRead.Wait();

                --available;
                temp = DequeueCore();
				++capacity;

				//avertir les threads attendant pour l'écriture
                canWrite.SignalAll();                
				return temp;
			}
		}
        /// <summary>
        /// Lit la valeur au début du buffer circulaire sans la retirer.
        /// </summary>
        /// <returns> Valeur lue. </returns>
        public override T Peek()
        {
            lock( syncExcl )
            {
                //attend qu'il y ait des blocs à lire
                canRead.Wait();

                //récupère le bloc depuis le buffer
                return buffer[ readPosition ];
            }
        }
        /// <summary>
        /// Essaie de retirer la valeur au début du buffer circulaire sans attendre si aucune valeur n'est présente dans le buffer.
        /// </summary>
        /// <param name="value"> Valeur retirée en cas de succès. </param>
        /// <returns> true si une valeur a été retirée, false sinon. </returns>
        public override bool TryDequeue( out T value )
        {
            if( Monitor.TryEnter( syncExcl ) )
            {
                try
                {
                    //regarde s'il y a une valeur à lire
                    if( available > 0 )
                    {
                        value = Dequeue();
                        return true;
                    }
                    else
                    {
                        value = default( T );
                        return false;
                    }
                }
                finally
                {
                    Monitor.Exit( syncExcl );
                }
            }
            else
            {
                value = default( T );
                return false;
            }
        }
		#endregion
	}
}