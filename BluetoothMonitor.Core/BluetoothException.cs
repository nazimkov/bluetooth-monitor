using System.Runtime.Serialization;

namespace BluetoothMonitor.Core
{
    public class BluetoothException : Exception
    {
        public BluetoothException()
        {
        }

        public BluetoothException(string? message) : base(message)
        {
        }

        public BluetoothException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BluetoothException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
