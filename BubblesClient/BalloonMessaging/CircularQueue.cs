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
        #region Fields
        protected readonly T[] buffer;
        protected readonly int size;
        protected int capacity;
        protected int available;
        protected int readPosition;
        protected int writePosition;
        readonly object syncExcl = new object();
        PredicateCondition canRead;
        PredicateCondition canWrite;
        #endregion
        #region Properties
        public int Size
        {
            get
            {
                return size;
            }
        }
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
        #region Constructor
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
        #region Implementation
        private void InitBuffer(int size, bool empty)
        {
            if (empty)
            {
                this.capacity = this.size;
                this.available = 0;
                this.readPosition = 0;
                this.writePosition = 0;
            }
            else
            {
                this.capacity = 0;
                this.available = this.size;
                this.readPosition = 0;
                this.writePosition = 0;
            }
        }
        public virtual void Enqueue(T value)
        {
            lock (syncExcl)
            {
                canWrite.Wait();

                --capacity;
                EnqueueCore(value);
                ++available;

                canRead.SignalAll();
            }
        }
        public virtual T Dequeue()
        {
            T temp;

            lock (syncExcl)
            {
                canRead.Wait();

                --available;
                temp = DequeueCore();
                ++capacity;

                canWrite.SignalAll();
                return temp;
            }
        }
        protected virtual void EnqueueCore(T value)
        {
            buffer[writePosition] = value;
            writePosition = (++writePosition % Size);
        }
        protected virtual T DequeueCore()
        {
            T value = buffer[readPosition];
            buffer[readPosition] = default(T);
            readPosition = (++readPosition % Size);
            return value;
        }
        public virtual T Peek()
        {
            lock (syncExcl)
            {
                canRead.Wait();
                return buffer[readPosition];
            }
        }
        public virtual bool TryDequeue(out T value)
        {
            if (Monitor.TryEnter(syncExcl))
            {
                try
                {
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
            if (Monitor.TryEnter(syncExcl))
            {
                try
                {
                    while (available > 0)
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

    public class PredicateCondition
    {
        #region Fields
        readonly ConditionPredicate predicate;
        readonly object conditionLock;
        #endregion
        #region Constructor
        public PredicateCondition(ConditionPredicate predicate)
            : this(predicate, new object())
        {
        }
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
        #region Implementation
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

    public delegate bool ConditionPredicate();
}