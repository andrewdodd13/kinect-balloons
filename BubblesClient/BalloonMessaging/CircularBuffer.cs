using System;
using System.IO;

namespace Balloons.Messaging
{
    /// <summary>
    /// Unsynchronized circular buffers of bytes.
    /// </summary>
    public class CircularBuffer
    {
        #region Fields
        /// <summary>
        /// Buffer.
        /// </summary>
        protected readonly byte[] buffer;
        /// <summary>
        /// Buffer size in bytes.
        /// </summary>
        protected readonly int size;
        /// <summary>
        /// Current reading position in the buffer.
        /// </summary>
        protected int readOffset;
        /// <summary>
        /// Current writing positin in the buffer.
        /// </summary>
        protected int writeOffset;
        /// <summary>
        /// How many bytes can be currently written to the buffer.
        /// </summary>
        protected int capacity;
        #endregion
        #region Properties
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
        /// Buffer size in bytes.
        /// </summary>
        public int Size
        {
            get
            {
                return size;
            }
        }
        /// <summary>
        /// How many bytes can be read between the current reading position and the end of the buffer.
        /// </summary>
        public int ForwardAvailable
        {
            get
            {
                return (size - Math.Max( capacity, readOffset ));
            }
        }
        /// <summary>
        /// How many bytes can be read between the start of the buffer and the current reading position.
        /// </summary>
        public int BackwardAvailable
        {
            get
            {
                return Math.Max( 0, readOffset - capacity );
            }
        }
        /// <summary>
        /// How many bytes can be read from the buffer, in total.
        /// </summary>
        public int Available
        {
            get
            {
                return (size - capacity);
            }
        }
        /// <summary>
        /// How many bytes can be written between the current writing position and the end of the buffer.
        /// </summary>
        public int ForwardCapacity
        {
            get
            {
                return Math.Min( capacity, size - writeOffset );
            }
        }
        /// <summary>
        /// How many bytes can be written between the start of the buffer and the current reading position.
        /// </summary>
        public int BackwardCapacity
        {
            get
            {
                return Math.Max( 0, capacity - size + writeOffset );
            }
        }
        /// <summary>
        /// How many bytes can be written to the buffer, in total.
        /// </summary>
        public int Capacity
        {
            get
            {
                return capacity;
            }
        }
        /// <summary>
        /// Current read position.
        /// </summary>
        public int ReadOffset
        {
            get
            {
                return readOffset;
            }
        }
        /// <summary>
        /// Current write position.
        /// </summary>
        public int WriteOffset
        {
            get
            {
                return writeOffset;
            }
        }
        #endregion
        #region Constructor
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
        #region Implementation
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
        /// Copy data from an array to the circular buffer.
        /// </summary>
        /// <param name="src"> Byte array containing the data to copy. </param>
        /// <param name="srcOffset"> Read position in the array. </param>
        /// <param name="dstOffset"> Write position in the buffer. </param>
        /// <param name="count"> How many bytes to copy. </param>
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
        ///  Copy data from the circular buffer to an array.
        /// </summary>
        /// <param name="dst"> Byte array where the data should be copied to. </param>
        /// <param name="dstOffset"> Write position in the array. </param>
        /// <param name="srcOffset"> Read position in the buffer. </param>
        /// <param name="count"> How many bytes to copy. </param>
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
        /// Read data from the circular buffer.
        /// </summary>
        /// <param name="buffer"> Byte array where the read data should be copied to. </param>
        /// <param name="offset"> Write position in the array </param>
        /// <param name="count"> How many bytes to read. </param>
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
            else if((count < 0) || (count > Available))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            ToBuffer(buffer, offset, readOffset, count);
            SkipRead(count);
        }
        /// <summary>
        /// Write data to the circular buffer
        /// </summary>
        /// <param name="buffer"> Byte array where the data to write should be copied from. </param>
        /// <param name="offset"> Where to start copying data from. </param>
        /// <param name="count"> How many bytes to write. </param>
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
            else if((count < 0) || (count > Capacity))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            FromBuffer(buffer, offset, writeOffset, count);
            SkipWrite(count);
        }
        /// <summary>
        /// Read the value of the first byte at the current reading position and move it one byte forward.
        /// </summary>
        public byte ReadByte()
        {
            byte b = PeekByte();
            SkipRead(1);
            return b;
        }
        /// <summary>
        /// Read the value of the first byte at the current reading position without changing that position.
        /// </summary>
        public byte PeekByte()
        {
            return PeekByte(0);
        }
        /// <summary>
        /// Read the value of one byte available to read without changing the current reading position.
        /// </summary>
        public byte PeekByte(int offset)
        {
            if(offset >= Available)
            {
                throw new InvalidOperationException();
            }
            int pos = readOffset + offset;
            while(pos >= size)
            {
                pos -= size;
            }
            return buffer[pos];
        }
        /// <summary>
        /// Write a byte to the buffer.
        /// </summary>
        /// <param name="data"> Byte to write. </param>
        public void WriteByte( byte data )
        {
            if(Capacity == 0)
            {
                throw new InvalidOperationException();
            }
            buffer[writeOffset] = data;
            SkipWrite(1);
        }
        /// <summary>
        /// Move the current reading position forward.
        /// </summary>
        /// <param name="count"> How many bytes to move the current position forward. </param>
        public void SkipRead(int count)
        {
            if((count < 0) || (count > Available))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            //avance le pointeur de lecture
            readOffset = ((readOffset + count) % size);
            //on peut maintenant écrire count octets en plus dans le buffer
            capacity += count;
        }
        /// <summary>
        /// Move the current writing position forward.
        /// </summary>
        /// <param name="count"> How many bytes to move the current position forward. </param>
        public void SkipWrite(int count)
        {
            if((count < 0) || (count > Capacity))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            //avance le pointeur d'écriture
            writeOffset = ((writeOffset + count) % size);
            //on peut maintenant écrire count octets en moins dans le buffer
            capacity -= count;
        }
        #endregion
    }
}
