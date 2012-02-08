using System;
using System.Collections.Generic;
using System.Threading;

namespace Balloons.Messaging
{
    /// <summary>
    /// Fixed-size buffer that can be used to synchronize data between producers and consumers.
    /// </summary>
    public class CircularQueue<T>
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
        readonly object syncExcl = new object();
        PredicateCondition canRead;
        PredicateCondition canWrite;
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
                lock (syncExcl)
                {
                    return available;
                }
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être écrit dans le buffer.
        /// </summary>
        public virtual bool Full
        {
            get
            {
                lock (syncExcl)
                {
                    return (capacity == 0);
                }
            }
        }
        /// <summary>
        /// Indique si aucun élément ne peut être lu dans le buffer.
        /// </summary>
        public virtual bool Empty
        {
            get
            {
                lock (syncExcl)
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
        public CircularQueue(int size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            else
            {
                this.size = size;
                this.buffer = new T[size];
                InitBuffer(size, true);
            }
            canRead = new PredicateCondition(delegate() { return available != 0; }, syncExcl);
            canWrite = new PredicateCondition(delegate() { return capacity != 0; }, syncExcl);
        }
        /// <summary>
        /// Crée un nouveau buffer circulaire à partir des éléments présents dans un tableau.
        /// </summary>
        /// <param name="array"> Tableau à partir duquel créer le buffer circulaire. </param>
        /// <param name="ownsArray"> Indique si le buffer utilise directement le tableau (true) ou copie son contenu dans un nouveau tableau (false). </param>
        /// <param name="empty"> Indique si le buffer doit être considéré comme vide ou plein à sa création. </param>
        /// <remarks> Si ownsArray vaut false, le tableau est copié dans le buffer en une opération rapide qui ne met pas en jeu de verrou. </remarks>
        public CircularQueue(T[] array, bool ownsArray, bool empty)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            else
            {
                this.size = array.Length;
                if (ownsArray)
                {
                    this.buffer = array;
                    InitBuffer(this.size, empty);
                }
                else
                {
                    this.buffer = new T[this.size];
                    Array.Copy(array, 0, buffer, 0, size);
                    InitBuffer(this.size, empty);
                }
            }
            canRead = new PredicateCondition(delegate() { return available != 0; }, syncExcl);
            canWrite = new PredicateCondition(delegate() { return capacity != 0; }, syncExcl);
        }
        #endregion
        #region Implémentation
        private void InitBuffer(int size, bool empty)
        {
            if (empty)
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
        /// Ajoute une valeur à la fin du buffer circulaire.
        /// </summary>
        /// <param name="value"> Valeur à ajouter. </param>
        public virtual void Enqueue(T value)
        {
            lock (syncExcl)
            {
                //attend qu'il y ait de la place dans le buffer
                canWrite.Wait();

                --capacity;
                EnqueueCore(value);
                ++available;

                //avertir les threads attendant pour la lecture
                canRead.SignalAll();
            }
        }
        /// <summary>
        /// Retire la valeur au début du buffer circulaire.
        /// </summary>
        /// <returns> Valeur retirée. </returns>
        public virtual T Dequeue()
        {
            T temp;

            lock (syncExcl)
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
        /// Copie une valeur dans le buffer à la position actuelle d'écriture et incrémente cette position.
        /// </summary>
        /// <param name="value"> Valeur à ajouter au buffer. </param>
        protected virtual void EnqueueCore(T value)
        {
            //copie l'élément dans le buffer
            buffer[writePosition] = value;
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
            value = buffer[readPosition];
            //réinitialise l'élément
            buffer[readPosition] = default(T);
            //puis on avance dans le buffer
            readPosition = (++readPosition % Size);
            return value;
        }
        /// <summary>
        /// Lit la valeur au début du buffer circulaire sans la retirer.
        /// </summary>
        /// <returns> Valeur lue. </returns>
        public virtual T Peek()
        {
            lock (syncExcl)
            {
                //attend qu'il y ait des blocs à lire
                canRead.Wait();

                //récupère le bloc depuis le buffer
                return buffer[readPosition];
            }
        }
        /// <summary>
        /// Essaie de retirer la valeur au début du buffer circulaire sans attendre si aucune valeur n'est présente dans le buffer.
        /// </summary>
        /// <param name="value"> Valeur retirée en cas de succès. </param>
        /// <returns> true si une valeur a été retirée, false sinon. </returns>
        public virtual bool TryDequeue(out T value)
        {
            if (Monitor.TryEnter(syncExcl))
            {
                try
                {
                    //regarde s'il y a une valeur à lire
                    if (available > 0)
                    {
                        value = Dequeue();
                        return true;
                    }
                    else
                    {
                        value = default(T);
                        return false;
                    }
                }
                finally
                {
                    Monitor.Exit(syncExcl);
                }
            }
            else
            {
                value = default(T);
                return false;
            }
        }
        /// <summary>
        /// Dequeue all items from the queue, without blocking.
        /// </summary>
        public virtual List<T> DequeueAll()
        {
            List<T> items = new List<T>();
            if(Monitor.TryEnter(syncExcl))
            {
                try
                {
                    while(available > 0)
                    {
                        --available;
                        items.Add(DequeueCore());
                        ++capacity;
                    }
                    canWrite.SignalAll();
                }
                finally
                {
                    Monitor.Exit(syncExcl);
                }
            }
            return items;
        }
        #endregion
    }

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
        public PredicateCondition(ConditionPredicate predicate)
            : this(predicate, new object())
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
        public PredicateCondition(ConditionPredicate predicate, object conditionLock)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            else if (conditionLock == null)
            {
                throw new ArgumentNullException("conditionLock");
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
            lock (conditionLock)
            {
                while (!predicate())
                {
                    Monitor.Wait(conditionLock);
                }
            }
        }
        /// <summary>
        /// Signale que la valeur de la condition peut avoir changé, si c'est le cas, réveille un seul thread attendant que la condition soit vérifiée.
        /// </summary>
        public void Signal()
        {
            lock (conditionLock)
            {
                if (predicate())
                {
                    Monitor.Pulse(conditionLock);
                }
            }
        }
        /// <summary>
        /// Signale que la valeur de la condition peut avoir changé, si c'est le cas, réveille tous les threads attendant que la condition soit vérifiée.
        /// </summary>
        public void SignalAll()
        {
            lock (conditionLock)
            {
                if (predicate())
                {
                    Monitor.PulseAll(conditionLock);
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