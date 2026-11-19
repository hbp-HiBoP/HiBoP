using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;

namespace HBP.Transfer.Transport
{
    /// <summary>Product messages must not expose localized operating-system exception text.</summary>
    public static class TransferError
    {
        public static string Describe(Exception error)
        {
            if (error is OperationCanceledException) return "Quest operation cancelled. Retry to continue.";
            if (error is TimeoutException) return "The Quest operation timed out. Check the connection and try again.";
            if (error is SocketException || error is IOException)
                return "The Quest transfer was interrupted. Check USB or Wi-Fi, then retry.";
            if (error is AuthenticationException) return "Quest authentication failed. Pair the headset again from Desktop.";
            if (error is InvalidOperationException || error is ArgumentException) return error.Message;
            return "The operation could not be completed. Check the application log, then try again.";
        }
    }
}
