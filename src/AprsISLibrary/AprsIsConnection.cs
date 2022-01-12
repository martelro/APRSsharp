﻿namespace AprsSharp.Connections.AprsIs
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Delegate for handling a full string from a TCP client.
    /// </summary>
    /// <param name="tcpMessage">The TCP message.</param>
    public delegate void HandleTcpString(string tcpMessage);

    /// <summary>
    /// This class initiates connections and performs authentication to the APRS internet service for receiving packets.
    /// It gives a user an option to use default credentials, filter and server or login with their specified user information.
    /// </summary>
    public class AprsIsConnection
    {
        private readonly ITcpConnection tcpConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AprsIsConnection"/> class.
        /// </summary>
        /// <param name="tcpConnection">An <see cref="ITcpConnection"/> to use for communication.</param>
        public AprsIsConnection(ITcpConnection tcpConnection)
        {
            if (tcpConnection == null)
            {
                throw new ArgumentNullException(nameof(tcpConnection));
            }

            this.tcpConnection = tcpConnection;
        }

        /// <summary>
        /// Event raised when TCP message is returned.
        /// </summary>
        public event HandleTcpString? ReceivedTcpMessage;

        /// <summary>
        /// The method to implement the authentication and receipt of APRS packets from APRS IS server.
        /// </summary>
        /// <param name="callsign">The users callsign string.</param>
        /// <param name="password">The users password string.</param>
        /// <param name="server">The APRS-IS server to contact.</param>
        /// <param name="filter">The APRS-IS filter string for server-side filtering.
        /// Null sends no filter, which is not recommended for most clients and servers.</param>
        /// <returns>An async task.</returns>
        public async Task Receive(string callsign, string password, string server, string? filter)
        {
            bool authenticated = false;

            string authString = $"user {callsign} pass {password} vers AprsSharp 0.1";
            if (filter != null)
            {
                authString += $" filter {filter}";
            }

            // Open connection
            tcpConnection.Connect(server, 14580);

           // Receive
            await Task.Run(() =>
            {
                while (true)
                {
                    string? received = tcpConnection.ReceiveString();

                    if (!string.IsNullOrEmpty(received))
                    {
                        ReceivedTcpMessage?.Invoke(received);

                        if (received.StartsWith('#'))
                        {
                            if (received.Contains("logresp"))
                            {
                                authenticated = true;
                            }

                            if (!authenticated)
                            {
                                tcpConnection.SendString(authString);
                            }
                        }
                    }

                    Thread.Sleep(500);
                }
            });
        }
    }
}
