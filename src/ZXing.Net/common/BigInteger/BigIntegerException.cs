using System;

namespace BigIntegerLibrary
{
   /// <summary>
   /// BigInteger-related exception class.
   /// </summary>
   [ZXing.Serializable]
   public sealed class BigIntegerException : Exception
   {
      /// <summary>
      /// BigIntegerException constructor.
      /// </summary>
      /// <param name="message">The exception message</param>
      /// <param name="innerException">The inner exception</param>
      public BigIntegerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }
}