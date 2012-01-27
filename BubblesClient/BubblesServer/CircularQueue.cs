using System;

namespace BubblesServer
{
    /// <summary>
    /// Buffer circulaire de taille fixe pouvant être utilisé pour synchroniser des données entre un ou plusieurs producteurs et consommateurs.
    /// </summary>
    public abstract class CircularQueue<T>
    {
        #region Champs
        /// <summary>
        /// Buffer.
        /// </summary>
		protected readonly T[] buffer;
		/// <summary>
		/// Taille du buffer.
		/// </summary>
        protected readonly int size;
        /// <summary>
		/// Nombre d'éléments pouvant être écrit dans le buffer.
		/// </summary>
		protected int capacity;
		/// <summary>
		/// Nombre d'éléments pouvant être lus depuis le buffer.
		/// </summary>
		protected int available;
        /// <summary>
        /// Position de lecture dans le buffer (prochain élément à être lu).
        /// </summary>
        protected int readPosition;
        /// <summary>
        /// Position d'écriture dans le buffer (prochain élément à être écrit).
        /// </summary>
        protected int writePosition;
        #endregion
        #region Propriétés
        /// <summary>
        /// Obtient la taille du buffer.
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
        }
        /// <summary>
        /// Nombre d'éléments dans le buffer.
        /// </summary>
        public virtual int Count
        {
            get
            {
                return available;
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être écrit dans le buffer.
        /// </summary>
        public virtual bool Full
        {
            get
            {
                return (capacity == 0);
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être lu dans le buffer.
        /// </summary>
        public virtual bool Empty
        {
            get
            {
                return (available == 0);
            }
        }
        #endregion
        #region Constructeur
        /// <summary>
        /// Crée un nouveau buffer circulaire avec la taille spécifiée.
        /// </summary>
        /// <param name="size"> Taille du buffer. </param>
        protected CircularQueue( int size )
        {
            if( size < 0 )
            {
                throw new ArgumentOutOfRangeException( "size"  );
            }
            else
            {
                this.size = size;
                this.buffer = new T[ size ];
                InitBuffer( size, true );
            }
        }
        /// <summary>
        /// Crée un nouveau buffer circulaire à partir des éléments présents dans un tableau.
        /// </summary>
        /// <param name="array"> Tableau à partir duquel créer le buffer circulaire. </param>
        /// <param name="ownsArray"> Indique si le buffer utilise directement le tableau (true) ou copie son contenu dans un nouveau tableau (false). </param>
        /// <param name="empty"> Indique si le buffer doit être considéré comme vide ou plein à sa création. </param>
        /// <remarks> Si ownsArray vaut false, le tableau est copié dans le buffer en une opération rapide qui ne met pas en jeu de verrou. </remarks>
        protected CircularQueue( T[] array, bool ownsArray, bool empty )
        {
            if( array == null )
            {
                throw new ArgumentNullException( "array" );
            }
            else
            {
                this.size = array.Length;
                if( ownsArray )
                {
                    this.buffer = array;
                    InitBuffer( this.size, empty );
                }
                else
                {
                    this.buffer = new T[ this.size ];
                    Array.Copy( array, 0, buffer, 0, size );
                    InitBuffer( this.size, empty );
                }
            }
        }
        #endregion
        #region Implémentation
        private void InitBuffer( int size, bool empty )
        {
            if( empty )
            {
                //tout le buffer pour écrire, rien à lire
                this.capacity = this.size;
                this.available = 0;
                this.readPosition = 0;
                this.writePosition = 0;
            }
            else
            {
                //plus de place pour écrire, tout le buffer à lire
                this.capacity = 0;
                this.available = this.size;
                this.readPosition = 0;
                this.writePosition = 0;
            }
        }
        /// <summary>
        /// En cas de substitution dans une classe dérivée, ajoute une valeur à la fin du buffer circulaire.
        /// </summary>
        /// <param name="value"> Valeur à ajouter. </param>
        public abstract void Enqueue( T value );
        /// <summary>
        /// En cas de substitution dans une classe dérivée, retire la valeur au début du buffer circulaire.
        /// </summary>
        /// <returns> Valeur retirée. </returns>
        public abstract T Dequeue();
        /// <summary>
        /// Copie une valeur dans le buffer à la position actuelle d'écriture et incrémente cette position.
        /// </summary>
        /// <param name="value"> Valeur à ajouter au buffer. </param>
        protected virtual void EnqueueCore( T value )
        {
            //copie l'élément dans le buffer
            buffer[ writePosition ] = value;
            //puis on avance dans le buffer
            writePosition = (++writePosition % Size);
        }
        /// <summary>
        /// Lit une valeur dans le buffer à la position actuelle de lecture et incrémente cette position.
        /// </summary>
        /// <returns> Valeur lue. </returns>
        protected virtual T DequeueCore()
        {
            T value;

            //lit l'élément depuis le buffer
            value = buffer[ readPosition ];
            //réinitialise l'élément
            buffer[ readPosition ] = default( T );
            //puis on avance dans le buffer
            readPosition = (++readPosition % Size);
            return value;
        }
        /// <summary>
        /// En cas de substitution dans une classe dérivée, lit la valeur au début du buffer circulaire sans la retirer.
        /// </summary>
        /// <returns> Valeur lue. </returns>
        public abstract T Peek();
        /// <summary>
        /// En cas de substitution dans une classe dérivée, essaie de retirer la valeur au début du buffer circulaire sans attendre si aucune valeur n'est présente dans le buffer.
        /// </summary>
        /// <param name="value"> Valeur retirée en cas de succès. </param>
        /// <returns> true si une valeur a été retirée, false sinon. </returns>
        public abstract bool TryDequeue( out T value );        
        #endregion
    }
}