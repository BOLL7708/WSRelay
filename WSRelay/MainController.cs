using BOLL7708;
using SuperSocket.WebSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using static BOLL7708.SuperServer;

namespace WSRelay
{
    internal class MainController
    {
        SuperServer _server = new SuperServer();
        ConcurrentDictionary<string, string> _channels = new();
        ConcurrentDictionary<string, string> _passwords = new();
        ConcurrentDictionary<string, bool> _authorizations = new();

        public Action<ServerStatus, int> StatusAction = (status, count) => { };

        public MainController() {
            _server.StatusAction += (status, count) =>
            {
                Debug.WriteLine(Enum.GetName(typeof(ServerStatus), status));
                StatusAction.Invoke(status, count);

            };
            _server.StatusMessageAction += (WebSocketSession? session, bool status, string message) => {
                if(!status) _channels.TryRemove(session?.SessionID ?? "", out string? oldSessionID);
            };
            _server.MessageReceivedAction += (WebSocketSession session, string message) =>
            {
                if (message.Length == 0) return;

                // Check if session is signed into a channel.
                // If the channel command was provided, we set the channel.
                // Error out if no channel.
                var hasChannel = _channels.TryGetValue(session.SessionID, out string? channel);
                var inChannel = GetCommandValue("CHANNEL", message);
                if(!hasChannel && inChannel.Length > 0)
                {
                    _channels[session.SessionID] = inChannel;
                    _server.SendMessage(session, $"SUCCESS: Connected for channel [{inChannel}]");
                    return;
                }
                if(inChannel.Length > 0)
                {
                    _server.SendMessage(session, $"SUCCESS: Already connected to channel [{inChannel}]");
                    return;
                }
                
                // We are in a channel.
                if(hasChannel && channel != null && channel.Length > 0)
                {
                    // Check if a password has been set for this channel.
                    // If not, but the password command was provided, we set the password.
                    var channelHasPassword = _passwords.TryGetValue(channel, out string? password);
                    var inPassword = GetCommandValue("PASSWORD", message);
                    if (!channelHasPassword && inPassword.Length > 0)
                    {
                        _passwords[channel] = inPassword;
                        _authorizations[session.SessionID] = true;
                        _server.SendMessage(session, $"SUCCESS: Set a password for channel [{channel}]");
                        return;
                    }

                    // If the channel has a password, check so this user has matched said password.
                    // Error out if there is no match.
                    var hasAuthorized = _authorizations.TryGetValue(session.SessionID, out bool authed);
                    if (channelHasPassword && (!hasAuthorized || !authed))
                    {
                        if(inPassword.Length > 0 && inPassword == password) // A password was supplied, try to match it.
                        {
                            _authorizations[session.SessionID] = true;
                            _server.SendMessage(session, $"SUCCESS: Authorized for channel [{channel}]");
                            return;
                        } else {
                            _server.SendMessage(session, $"ERROR: Not authorized for channel [{channel}]");
                            return;
                        }
                    }
                    if (inPassword.Length > 0)
                    {
                        _server.SendMessage(session, $"SUCCESS: Already authorized for channel [{channel}]");
                        return;
                    }

                    // Broadcast to channel
                    List<string> sessionIDs = new();
                    foreach(string key in _channels.Keys)
                    {
                        if (_channels[key] == channel && key != session.SessionID)
                        {
                            sessionIDs.Add(key);
                        }
                    }
                    _server.SendMessageToGroup(sessionIDs.ToArray(), message);
                    return;
                } else
                {
                    // Not yet connected to a channel, so we error out.
                    _server.SendMessage(session, "ERROR: Not connected to any channel");
                    return;
                }
            };
        }

        public void StartOrRestartServer(int port) {
            _server.Start(port);
        }

        private string GetCommandValue(string command, string message)
        {
            if(message.StartsWith($"{command}:"))
            {
                var arr = message.Split(':');
                if(arr.Length == 2 && arr[0] == command)
                {
                    var value = arr[1].Trim();
                    return value;
                }
            }
            return "";
        }
    }
}