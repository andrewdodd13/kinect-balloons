using System;
using System.IO;

namespace Balloons.Messaging
{
    /// <summary>
    /// Unsynchronized circular buffers of bytes.
    /// </summary>
    public class CircularBuffer
    {
        #region Champs
        /// <summary>
        /// Buffer.
        /// </summary>
        protected readonly byte[] buffer;
        /// <summary>
        /// Taille du buffer, en octets.
        /// </summary>
        protected readonly int size;
        /// <summary>
        /// Pointeur de lecture dans le buffer.
        /// </summary>
        protected int readOffset;
        /// <summary>
        /// Pointeur d'écriture dans le buffer.
        /// </summary>
        protected int writeOffset;
        /// <summary>
        /// Nombre d'octets pouvant actuellement être écrit dans le buffer.
        /// </summary>
        protected int capacity;
        #endregion
        #region Propriétés
        /// <summary>
        /// Taille, en octets, du buffer.
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
        }
        /// <summary>
        /// Nombre d'octets disponibles à la lecture entre le pointeur de lecture et la fin du buffer.
        /// </summary>
        public int ForwardAvailable
        {
            get
            {
                return (size - Math.Max( capacity, readOffset ));
            }
        }
        /// <summary>
        /// Nombre d'octets disponibles à la lecture entre le début du buffer et le pointeur de lecture.
        /// </summary>
        public int BackwardAvailable
        {
            get
            {
                return Math.Max( 0, readOffset - capacity );
            }
        }
        /// <summary>
        /// Nombre total d'octets disponibles à la lecture dans le buffer.
        /// </summary>
        public int Available
        {
            get
            {
                return (size - capacity);
            }
        }
        /// <summary>
        /// Nombre d'octets disponibles à l'écriture entre le pointeur d'écriture et la fin du buffer.
        /// </summary>
        public int ForwardCapacity
        {
            get
            {
                return Math.Min( capacity, size - writeOffset );
            }
        }
        /// <summary>
        /// Nombre d'octets disponibles à l'écriture entre le début du buffer et le pointeur d'écriture.
        /// </summary>
        public int BackwardCapacity
        {
            get
            {
                return Math.Max( 0, capacity - size + writeOffset );
            }
        }
        /// <summary>
        /// Nombre total d'octets disponibles à l'écriture dans le buffer.
        /// </summary>
        public int Capacity
        {
            get
            {
                return capacity;
            }
        }
        /// <summary>
        /// Underlying buffer.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return buffer;
            }
        }
        /// <summary>
        /// Current read offset.
        /// </summary>
        public int ReadOffset
        {
            get
            {
                return readOffset;
            }
        }
        /// <summary>
        /// Current write offset.
        /// </summary>
        public int WriteOffset
        {
            get
            {
                return writeOffset;
            }
        }
        #endregion
        #region Constructeur
        /// <summary>
		/// Create a new circular buffer with the given size in bytes.
		/// </summary>
		/// <param name="size"> Buffer size. </param>
		public CircularBuffer( int size )
		{
			if( size >= 0 )
			{
				buffer = new byte[ size ];
				this.size = size;
			}
			else
			{
				throw new ArgumentOutOfRangeException( "size" );
			}
            readOffset = 0;
			writeOffset = 0;
            capacity = size;
		}
        #endregion
        #region Implémentation
        private static void GetChunks( int offset, int bufferSize, int size, out int firstChunk, out int lastChunk )
        {
            if( offset >= bufferSize )
            {
                firstChunk = 0;
                lastChunk = size;
            }
            else if( (offset + size) > bufferSize )
            {
                firstChunk = (bufferSize - offset);
                lastChunk = (size - firstChunk);
            }
            else
            {
                firstChunk = size;
                lastChunk = 0;
            }
        }
        /// <summary>
        /// Copie des données dans le buffer circulaire depuis un tableau.
        /// </summary>
        /// <param name="src"> Tableau d'octets contenant les données à copier. </param>
        /// <param name="srcOffset"> Offset du début des données à copier. </param>
        /// <param name="dstOffset"> Offset du buffer circulaire auquel commencer la copie. </param>
        /// <param name="count"> Nombre d'octets à copier. </param>
        protected void FromBuffer( byte[] src, int srcOffset, int dstOffset, int count )
        {
            int firstChunk;
            int lastChunk;

            if( src == null )
            {
                throw new ArgumentNullException( "src" );
            }
            else if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            else if( (srcOffset < 0) || ((srcOffset + count) > src.Length) )
            {
                throw new ArgumentOutOfRangeException( "srcOffset" );
            }
            else if( (dstOffset < 0) || (dstOffset >= this.size) )
            {
                throw new ArgumentOutOfRangeException( "dstOffset" );
            }

            if( count > 0 )
            {
                GetChunks( dstOffset, this.size, count, out firstChunk, out lastChunk );
                if( lastChunk > 0 )
                {
                    System.Buffer.BlockCopy( src, srcOffset, this.buffer, dstOffset, firstChunk );
                    System.Buffer.BlockCopy( src, srcOffset + firstChunk, this.buffer, 0, lastChunk );
                }
                else
                {
                    System.Buffer.BlockCopy( src, srcOffset, this.buffer, dstOffset, count );
                }
            }
        }
        /// <summary>
        /// Copie des données dans un tableau depuis le buffer circulaire.
        /// </summary>
        /// <param name="dst"> Tableau d'octets dans lequel copier des données. </param>
        /// <param name="dstOffset"> Offset du tableau auquel commencer la copie. </param>
        /// <param name="srcOffset"> Offset du buffer circulaire du début des données à copier. </param>
        /// <param name="count"> Nombre d'octets à copier. </param>
        protected void ToBuffer( byte[] dst, int dstOffset, int srcOffset, int count )
        {
            int firstChunk;
            int lastChunk;

            if( dst == null )
            {
                throw new ArgumentNullException( "dst" );
            }
            else if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            else if( (dstOffset < 0) || ((dstOffset + count) > dst.Length) )
            {
                throw new ArgumentOutOfRangeException( "dstOffset" );
            }
            else if( (srcOffset < 0) || (srcOffset >= this.size) )
            {
                throw new ArgumentOutOfRangeException( "srcOffset" );
            }

            if( count > 0 )
            {
                GetChunks( srcOffset, this.size, count, out firstChunk, out lastChunk );
                if( (lastChunk != 0) && (firstChunk != 0) )
                {
                    System.Buffer.BlockCopy( this.buffer, srcOffset, dst, dstOffset, firstChunk );
                    System.Buffer.BlockCopy( this.buffer, 0, dst, dstOffset + firstChunk, lastChunk );
                }
                else if( firstChunk == 0 )
                {
                    System.Buffer.BlockCopy( this.buffer, (srcOffset % this.size), dst, dstOffset, lastChunk );
                }
                else
                {
                    System.Buffer.BlockCopy( this.buffer, srcOffset, dst, dstOffset, count );
                }
            }
        }
        /// <summary>
        /// Copie des données dans le buffer circulaire depuis un flux.
        /// </summary>
        /// <param name="s"> Flux contenant les données à copier. </param>
        /// <param name="dstOffset"> Offset du buffer circulaire auquel commencer la copie. </param>
        /// <param name="count"> Nombre d'octets à copier. </param>
        protected void FromStream( Stream s, int dstOffset, int count )
        {
            int firstChunk;
            int lastChunk;

            if( s == null )
            {
                throw new ArgumentNullException( "s" );
            }
            else if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            else if( (dstOffset < 0) || (dstOffset >= this.size) )
            {
                throw new ArgumentOutOfRangeException( "dstOffset" );
            }

            if( count > 0 )
            {
                GetChunks( dstOffset, this.size, count, out firstChunk, out lastChunk );
                if( lastChunk != 0 )
                {
                    s.Read( this.buffer, dstOffset, firstChunk );
                    s.Read( this.buffer, dstOffset + firstChunk, lastChunk );
                }
                else
                {
                    s.Read( this.buffer, dstOffset, count );
                }
            }
        }
        /// <summary>
        /// Copie des données dans un flux depuis le buffer circulaire.
        /// </summary>
        /// <param name="srcOffset"> Offset du buffer circulaire auquel commencer la copie. </param>
        /// <param name="s"> Flux contenant les données à copier. </param>
        /// <param name="count"> Nombre d'octets à copier. </param>
        protected void ToStream( int srcOffset, Stream s, int count )
        {
            int firstChunk;
            int lastChunk;

            if( s == null )
            {
                throw new ArgumentNullException( "s" );
            }
            else if( count < 0 )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            else if( (srcOffset < 0) || (srcOffset >= this.size) )
            {
                throw new ArgumentOutOfRangeException( "srcOffset" );
            }

            if( count > 0 )
            {
                GetChunks( srcOffset, this.size, count, out firstChunk, out lastChunk );
                if( lastChunk != 0 )
                {
                    s.Write( this.buffer, srcOffset, firstChunk );
                    s.Write( this.buffer, srcOffset + firstChunk, lastChunk );
                }
                else
                {
                    s.Write( this.buffer, srcOffset, count );
                }
            }
        }
        /// <summary>
        /// Lit des données depuis le buffer circulaire.
        /// </summary>
        /// <param name="buffer"> Buffer dans lequel copier les données lues. </param>
        /// <param name="offset"> Offset du buffer auquel copier les données lues. </param>
        /// <param name="count"> Nombre d'octets à lire. </param>
        public void Read( byte[] buffer, int offset, int count )
        {
            if( buffer == null )
            {
                throw new ArgumentNullException( "buffer" );
            }
            else if( (offset < 0) || ((offset + count) > buffer.Length) )
            {
                throw new ArgumentOutOfRangeException( "offset" );
            }
            Read( buffer, 0, offset, count, true );
        }
        /// <summary>
        /// Ecrit des données dans le buffer circulaire.
        /// </summary>
        /// <param name="buffer"> Buffer contenant les données à écrire. </param>
        /// <param name="offset"> offset du début des données à écrire. </param>
        /// <param name="count"> Nombre d'octets à écrire. </param>
        public void Write( byte[] buffer, int offset, int count )
        {
            if( buffer == null )
            {
                throw new ArgumentNullException( "buffer" );
            }
            else if( (offset < 0) || ((offset + count) > buffer.Length) )
            {
                throw new ArgumentOutOfRangeException( "offset" );
            }
            Write( buffer, offset, 0, count, true );
        }
        /// <summary>
        /// Lit un octet depuis le buffer circulaire.
        /// </summary>
        /// <returns> Octet lu. </returns>
        public byte ReadByte()
        {
            return ReadByte( 0, true );
        }
        /// <summary>
        /// Ecrit un octet dans le buffer
        /// </summary>
        /// <param name="data"> Octet à écrire. </param>
        public void WriteByte( byte data )
        {
            WriteByte( data, 0, true );
        }
        /// <summary>
        /// Crée un nouveau flux de lecture pour le <see cref="CircularBuffer"/>.
        /// </summary>
        /// <returns> Flux de lecture créé. </returns>
        public virtual CircularBuffer.ReadStream CreateReadStream()
        {
            return new ReadStream( this );
        }
        /// <summary>
        /// Crée un nouveau flux d'écriture pour le <see cref="CircularBuffer"/>.
        /// </summary>
        /// <returns> Flux d'écriture créé. </returns>
        public virtual CircularBuffer.WriteStream CreateWriteStream()
        {
            return new WriteStream( this );
        }
        #endregion
        #region Méthodes abstraites
        /// <summary>
        /// Lit des données depuis le buffer circulaire et les copie dans un tableau d'octets.
        /// </summary>
        /// <param name="buffer"> Tableau d'octets dans lequel copier les données lues. </param>
        /// <param name="srcOffset"> Offset du début des données à lire, par rapport à <see cref="writeOffset"/>. </param>
        /// <param name="dstOffset"> Offset du buffer auquel copier les données lues. </param>
        /// <param name="count"> Nombre d'octets à lire. </param>
        /// <param name="changePos"> Indique s'il faut avancer la position de lecture dans le buffer après l'appel à <see cref="Read(byte[], int, int, int, bool)"/>. </param>
        protected virtual void Read( byte[] buffer, int srcOffset, int dstOffset, int count, bool changePos )
        {
            if( (count < 0) || (count > Available) )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            //lit les données depuis le buffer circulaire
            ToBuffer( buffer, dstOffset, readOffset + srcOffset, count );
            if( changePos )
            {
                SkipRead( count + srcOffset );
            }
        }
        /// <summary>
        /// Lit des données depuis un tableau d'octets et les écrit des données dans le buffer circulaire.
        /// </summary>
        /// <param name="buffer"> Tableau d'octets contenant les données à écrire. </param>
        /// <param name="srcOffset"> Offset du début des données à lire. </param>
        /// <param name="dstOffset"> Offset du début des données à écrire, par rapport à <see cref="readOffset"/>. </param>
        /// <param name="count"> Nombre d'octets à écrire. </param>
        /// <param name="changePos"> Indique s'il faut avancer la position d'écriture dans le buffer après l'appel à <see cref="Write(byte[], int, int, int, bool)"/>. </param>
        protected virtual void Write( byte[] buffer, int srcOffset, int dstOffset, int count, bool changePos )
        {
            if( (count < 0) || (count > Capacity) )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            //écrit les données dans le buffer circulaire
            FromBuffer( buffer, srcOffset, writeOffset + dstOffset, count );
            if( changePos )
            {
                SkipWrite( count + dstOffset );
            }
        }
        /// <summary>
        /// Lit un octet depuis le buffer circulaire.
        /// </summary>
        /// <param name="offset"> Offset du début des données à lire, par rapport à <see cref="writeOffset"/>. </param>
        /// <param name="changePos"> Indique s'il faut avancer la position de lecture dans le buffer après l'appel à <see cref="ReadByte(int, bool)"/>. </param>
        /// <returns> Octet lu. </returns>
        protected virtual byte ReadByte( int offset, bool changePos )
        {
            if( Available == 0 )
            {
                throw new InvalidOperationException();
            }
            int pos = readOffset + offset;
            while(pos >= size)
            {
                pos -= size;
            }
            byte data = buffer[ pos ];

            if( changePos )
            {
                SkipRead( 1 + offset );
            }
            return data;
        }
        /// <summary>
        /// Ecrit un octet dans le buffer
        /// </summary>
        /// <param name="data"> Octet à écrire. </param>
        /// <param name="offset"> Offset auquel à écrire l'octet, par rapport à <see cref="readOffset"/>. </param>
        /// <param name="changePos"> Indique s'il faut avancer la position d'écriture dans le buffer après l'appel à <see cref="WriteByte(byte, int, bool)"/>. </param>
        protected virtual void WriteByte( byte data, int offset, bool changePos )
        {
            if( Capacity == 0 )
            {
                throw new InvalidOperationException();
            }
            buffer[ writeOffset + offset ] = data;
            if( changePos )
            {
                SkipWrite( 1 + offset );
            }
        }
        /// <summary>
        /// Avance la position de lecture dans le buffer.
        /// </summary>
        /// <param name="count"> Nombre d'octets duquel augmenter la position de lecture du buffer. </param>
        public virtual void SkipRead( int count )
        {
            if( (count < 0) || (count > Available) )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            //avance le pointeur de lecture
            readOffset = ((readOffset + count) % size);
            //on peut maintenant écrire count octets en plus dans le buffer
            capacity += count;
        }
        /// <summary>
        /// Avance la position d'écriture dans le buffer.
        /// </summary>
        /// <param name="count"> Nombre d'octets duquel augmenter la position d'écriture du buffer. </param>
        public virtual void SkipWrite( int count )
        {
            if( (count < 0) || (count > Capacity) )
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            //avance le pointeur d'écriture
            writeOffset = ((writeOffset + count) % size);
            //on peut maintenant écrire count octets en moins dans le buffer
            capacity -= count;
        }
        #endregion
        #region ReadStream
        /// <summary>
        /// Permet de lire des données depuis un <see cref="CircularBuffer"/> comme si c'était un flux.
        /// </summary>
        public class ReadStream : Stream
        {
            #region Champs
            CircularBuffer queue;
            int pos;
            bool closed;
            #endregion
            #region Propriétés
            /// <summary>
            /// Obtient le <see cref="CircularBuffer"/> sous-jacent.
            /// </summary>
            public CircularBuffer Queue
            {
                get
                {
                    return queue;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge la lecture.
            /// </summary>
            /// <value> true si le flux est ouvert, false sinon. </value>
            public override bool CanRead
            {
                get
                {
                    return !closed;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge l'écriture.
            /// </summary>
            /// <value> false. </value>
            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge la recherche.
            /// </summary>
            /// <value> true si le flux est ouvert, false sinon. </value>
            public override bool CanSeek
            {
                get
                {
                    return !closed;
                }
            }
            /// <summary>
            /// Indique le nombre d'octets disponibles à la lecture dans le flux, soit <see cref="CircularBuffer.Available"/>.
            /// </summary>
            public override long Length
            {
                get
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    return queue.Available;
                }
            }
            /// <summary>
            /// Indique la position actuelle dans le flux.
            /// </summary>
            public override long Position
            {
                get
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    return pos;
                }
                set
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    Seek( value, SeekOrigin.Begin );
                }
            }
            #endregion
            #region Constructeur
            /// <summary>
            /// Crée un nouveau flux de lecture pour <see cref="CircularBuffer"/>.
            /// </summary>
            /// <param name="queue"> Buffer circulaire pour lequel créer un flux de lecture. </param>
            protected internal ReadStream( CircularBuffer queue )
            {
                if( queue == null )
                {
                    throw new ArgumentNullException( "queue" );
                }
                this.queue = queue;
                pos = 0;
            }
            #endregion
            #region Implémentation
            /// <summary>
            /// Définit la position actuelle dans le flux.
            /// </summary>
            /// <param name="offset"> Offset par rapport à origin. </param>
            /// <param name="origin"> Valeur indiquant le point de référence utilisé pour obtenir la nouvelle position. </param>
            /// <returns> Nouvelle position dans le flux. </returns>
            public override long Seek( long offset, SeekOrigin origin )
            {
                long newOffset;
                long length;

                if( closed )
                {
                    throw ThrowDisposed();
                }
                length = queue.Available;
                switch( origin )
                {
                    default:
                    case SeekOrigin.Begin:
                    {
                        newOffset = offset;
                        break;
                    }
                    case SeekOrigin.Current:
                    {
                        newOffset = pos + offset;
                        break;
                    }
                    case SeekOrigin.End:
                    {
                        newOffset = length - offset;
                        break;
                    }
                }
                if( (newOffset < 0) || (newOffset > length) )
                {
                    throw new ArgumentOutOfRangeException( "offset" );
                }
                else
                {                    
                    return (pos = (int)newOffset);
                }
            }
            /// <summary>
            /// Lit une séquence d’octets à partir du flux et avance la position dans le flux du nombre d’octets lus.
            /// </summary>
            /// <param name="buffer"> Tableau d'octets dans lequel copier les données lues. </param>
            /// <param name="offset"> Offset du début des données à lire depuis le flux. </param>
            /// <param name="count"> Nombre d'octets à lire. </param>
            /// <returns> Nombre d'octets lus. </returns>
            /// <remarks> L'appel à <see cref="Read"/> ne bloque jamais. Si moins de count octets sont disponibles à lecture dans le buffer seulement ces octets seront lus. </remarks>
            public override int Read( byte[] buffer, int offset, int count )
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                else if( buffer == null )
                {
                    throw new ArgumentNullException( "buffer" );
                }
                else if( offset < 0 )
                {
                    throw new ArgumentOutOfRangeException( "offset" );
                }
                else if( count < 0 )
                {
                    throw new ArgumentOutOfRangeException( "count" );
                }
                else if( (offset + count) > buffer.Length )
                {
                    throw new ArgumentOutOfRangeException();
                }
                else
                {
                    int left = (queue.Available - pos);

                    if( (left == 0) || (count == 0) )
                    {
                        return 0;
                    }
                    else
                    {
                        if( left < count )
                        {
                            count = left;
                        }
                        queue.Read( buffer, pos, offset, count, false );                        
                        pos += count;
                        return count;
                    }
                }
            }
            /// <summary>
            /// Lit un octet à partir du flux et avance la position dans le flux d'un octet.
            /// </summary>
            /// <returns> Valeur de l'octet lu, ou -1 s'il se situe à la fin du flux. </returns>
            /// <remarks> L'appel à <see cref="ReadByte"/> ne bloque jamais. Si aucun octet n'est disponible à lecture dans le buffer, <see cref="ReadByte"/> renvoie -1. </remarks>
            public override int ReadByte()
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                if( (pos + 1) > queue.Available )
                {
                    return -1;
                }
                else
                {
                    return queue.ReadByte( pos++, false );
                }
            }
            /// <summary>
            /// Si des opérations de lecture ont été effectuées sur le flux, avance la position de lecture dans le <see cref="CircularBuffer"/> et réinitialise <see cref="Position"/>.
            /// </summary>
            public override void Flush()
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                if( pos > 0 )
                {
                    queue.SkipRead( pos );                    
                    pos = 0;
                }
            }
            /// <summary>
            /// Ferme le flux et avance la position de lecture dans le <see cref="CircularBuffer"/> si nécessaire.
            /// </summary>
            public override void Close()
            {
                if( !closed )
                {
                    Flush();
                    closed = true;
                }
            }
            private Exception ThrowDisposed()
            {
                return new ObjectDisposedException( GetType().FullName );
            }
            #endregion
            #region Non supporté
            /// <summary>
            /// <see cref="Write"/> n'est pas supporté par <see cref="ReadStream"/>.
            /// </summary>
            public override void Write( byte[] buffer, int offset, int count )
            {
                throw new NotSupportedException();
            }
            /// <summary>
            /// <see cref="WriteByte"/> n'est pas supporté par <see cref="ReadStream"/>.
            /// </summary>
            public override void WriteByte( byte value )
            {
                throw new NotSupportedException();
            }
            /// <summary>
            /// <see cref="SetLength"/> n'est pas supporté par <see cref="ReadStream"/>.
            /// </summary>
            public override void SetLength( long value )
            {
                throw new NotSupportedException();
            }
            #endregion
        }
        #endregion
        #region WriteStream
        /// <summary>
        /// Permet d'écrire des données dans un <see cref="CircularBuffer"/> comme si c'était un flux.
        /// </summary>
        public class WriteStream : Stream
        {
            #region Champs
            CircularBuffer queue;
            int pos;
            bool closed;
            #endregion
            #region Propriétés
            /// <summary>
            /// Obtient le <see cref="CircularBuffer"/> sous-jacent.
            /// </summary>
            public CircularBuffer Queue
            {
                get
                {
                    return queue;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge la lecture.
            /// </summary>
            /// <value> false. </value>
            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge l'écriture.
            /// </summary>
            /// <value> true si le flux est ouvert, false sinon. </value>
            public override bool CanWrite
            {
                get
                {
                    return !closed;
                }
            }
            /// <summary>
            /// Indique si le flux prend en charge la recherche.
            /// </summary>
            /// <value> true si le flux est ouvert, false sinon. </value>
            public override bool CanSeek
            {
                get
                {
                    return !closed;
                }
            }
            /// <summary>
            /// Indique le nombre d'octets disponibles à l'écriture dans le flux, soit <see cref="CircularBuffer.Capacity"/>.
            /// </summary>
            public override long Length
            {
                get
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    return queue.Available;
                }
            }
            /// <summary>
            /// Indique la position actuelle dans le flux.
            /// </summary>
            public override long Position
            {
                get
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    return pos;
                }
                set
                {
                    if( closed )
                    {
                        throw ThrowDisposed();
                    }
                    Seek( value, SeekOrigin.Begin );
                }
            }
            #endregion
            #region Constructeur
            /// <summary>
            /// Crée un nouveau flux d'écriture pour <see cref="CircularBuffer"/>.
            /// </summary>
            /// <param name="queue"> Buffer circulaire pour lequel créer un flux d'écriture. </param>
            protected internal WriteStream( CircularBuffer queue )
            {
                if( queue == null )
                {
                    throw new ArgumentNullException( "queue" );
                }
                this.queue = queue;
                pos = 0;
            }
            #endregion
            #region Implémentation
            /// <summary>
            /// Définit la position actuelle dans le flux.
            /// </summary>
            /// <param name="offset"> Offset par rapport à origin. </param>
            /// <param name="origin"> Valeur indiquant le point de référence utilisé pour obtenir la nouvelle position. </param>
            /// <returns> Nouvelle position dans le flux. </returns>
            public override long Seek( long offset, SeekOrigin origin )
            {
                long newOffset;
                long length;

                if( closed )
                {
                    throw ThrowDisposed();
                }
                length = queue.Available;
                switch( origin )
                {
                    default:
                    case SeekOrigin.Begin:
                    {
                        newOffset = offset;
                        break;
                    }
                    case SeekOrigin.Current:
                    {
                        newOffset = pos + offset;
                        break;
                    }
                    case SeekOrigin.End:
                    {
                        newOffset = length - offset;
                        break;
                    }
                }
                if( (newOffset < 0) || (newOffset > length) )
                {
                    throw new ArgumentOutOfRangeException( "offset" );
                }
                else
                {
                    return (pos = (int)newOffset);
                }
            }
            /// <summary>
            /// Ecrit une séquence d’octets dans le flux flux et avance la position dans le flux du nombre d’octets écrits.
            /// </summary>
            /// <param name="buffer"> Tableau d'octets à écrire dans le flux. </param>
            /// <param name="offset"> Offset du début des données à écrire dans le flux. </param>
            /// <param name="count"> Nombre d'octets à écrire. </param>
            public override void Write( byte[] buffer, int offset, int count )
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                else if( buffer == null )
                {
                    throw new ArgumentNullException( "buffer" );
                }
                else if( offset < 0 )
                {
                    throw new ArgumentOutOfRangeException( "offset" );
                }
                else if( count < 0 )
                {
                    throw new ArgumentOutOfRangeException( "count" );
                }
                else if( (offset + count) > buffer.Length )
                {
                    throw new ArgumentOutOfRangeException();
                }
                else
                {
                    queue.Write( buffer, pos, offset, count, false );
                    pos += count;
                }
            }
            /// <summary>
            /// Ecrit un octet à partir du flux et avance la position dans le flux d'un octet.
            /// </summary>
            public override void WriteByte( byte value )
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                queue.WriteByte( value, pos++, false );
            }
            /// <summary>
            /// Si des opérations d'écriture ont été effectuées sur le flux, avance la position d'écriture dans le <see cref="CircularBuffer"/> et réinitialise <see cref="Position"/>.
            /// </summary>
            public override void Flush()
            {
                if( closed )
                {
                    throw ThrowDisposed();
                }
                if( pos > 0 )
                {
                    queue.SkipWrite( pos );
                    pos = 0;
                }
            }
            /// <summary>
            /// Ferme le flux et avance la position d'écriture dans le <see cref="CircularBuffer"/> si nécessaire.
            /// </summary>
            public override void Close()
            {
                if( !closed )
                {
                    Flush();
                    closed = true;
                }
            }
            private Exception ThrowDisposed()
            {
                return new ObjectDisposedException( GetType().FullName );
            }
            #endregion
            #region Non supporté
            /// <summary>
            /// <see cref="Read"/> n'est pas supporté par <see cref="WriteStream"/>.
            /// </summary>
            public override int Read( byte[] buffer, int offset, int count )
            {
                throw new NotSupportedException();
            }
            /// <summary>
            /// <see cref="ReadByte"/> n'est pas supporté par <see cref="WriteStream"/>.
            /// </summary>
            public override int ReadByte()
            {
                throw new NotSupportedException();
            }
            /// <summary>
            /// <see cref="SetLength"/> n'est pas supporté par <see cref="WriteStream"/>.
            /// </summary>
            public override void SetLength( long value )
            {
                throw new NotSupportedException();
            }
            #endregion
        }
        #endregion
    }
}
