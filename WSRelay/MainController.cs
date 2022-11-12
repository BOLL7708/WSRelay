using BOLL7708;
using SuperSocket.WebSocket.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static BOLL7708.SuperServer;

namespace WSRelay
{
    internal class MainController
    {
        SuperServer _server = new SuperServer();
        ConcurrentDictionary<string, bool> _sessions = new();
        ConcurrentDictionary<string, string> _channels = new();
        ConcurrentDictionary<string, string> _passwords = new();
        ConcurrentDictionary<string, bool> _authorizations = new();

        public Action<ServerStatus, int> StatusAction = (status, count) => { };
        private const string PREFIX = ":::";

        public MainController() {
            _server.StatusAction += (status, count) =>
            {
                StatusAction.Invoke(status, count);
            };
            _server.StatusMessageAction += (WebSocketSession? session, bool status, string message) => {
                if(status)
                {
                    if (session != null)
                    {
                        _sessions[session.SessionID] = true;
                        _server.SendMessage(session, $"{PREFIX}SUCCESS:0:Connected to general channel");
                    }
                } 
                else if(session != null)
                {
                    _sessions.TryRemove(session.SessionID, out bool oldBool);
                    _channels.TryRemove(session.SessionID, out string? oldString);
                }
            };
            _server.MessageReceivedAction += (WebSocketSession session, string message) =>
            {
                // Empty message is ignored.
                if (message.Length == 0) return;

                // Check if session is signed into a channel.
                // If the channel command was provided, we set the channel.
                var hasChannel = _channels.TryGetValue(session.SessionID, out string? channel);
                if (channel?.Length == 0) channel = null; // Make sure it becomes null if empty, as now it could be either, easy to null-check.
                var inputChannel = GetCommandValue("CHANNEL", message, true, true);

                // Not in a channel, but provided one.
                if (!hasChannel && inputChannel.Length > 0)
                {
                    _channels[session.SessionID] = inputChannel;
                    _server.SendMessage(session, $"{PREFIX}SUCCESS:10:Connected to #{inputChannel}");
                    return;
                }
                
                // In a channel, but also provided one.
                if (inputChannel.Length > 0 && channel != null && channel.Length > 0)
                {
                    if(channel == inputChannel)
                    {
                        _server.SendMessage(session, $"{PREFIX}SUCCESS:11:Already connected to #{channel}");
                    } 
                    else
                    {
                        _server.SendMessage(session, $"{PREFIX}ERROR:50:Cannot connect to a second channel");
                    }
                    return;
                }
                
                // Check if a password has been set for this channel, if we are in one.
                // If not, but the password command was provided, we set the password.
                var channelHasPassword = _passwords.TryGetValue(channel ?? "", out string? password);
                var inputPassword = GetCommandValue("PASSWORD", message);
                
                // Set password for channel if we are in one and a password was provided
                if (channel != null && !channelHasPassword && inputPassword.Length > 0)
                {
                    _passwords[channel] = inputPassword;
                    _authorizations[session.SessionID] = true;
                    _server.SendMessage(session, $"{PREFIX}SUCCESS:20:Password set for #{channel}");
                    return;
                }

                // If the channel has a password, check so this user has matched said password.
                // Error out if there is no match.
                var hasAuthorized = _authorizations.TryGetValue(session.SessionID, out bool authed);
                
                // Try to authorize for channel, if we are in one.
                if (channel != null && channelHasPassword && (!hasAuthorized || !authed))
                {
                    if(inputPassword.Length > 0 && inputPassword == password) // A password was supplied, try to match it.
                    {
                        _authorizations[session.SessionID] = true;
                        _server.SendMessage(session, $"{PREFIX}SUCCESS:21:Authorized for #{channel}");
                        return;
                    } else {
                        _server.SendMessage(session, $"{PREFIX}ERROR:51:Not authorized for #{channel}");
                        return;
                    }
                }
                if (channel != null && inputPassword.Length > 0)
                {
                    _server.SendMessage(session, $"{PREFIX}SUCCESS:22:Already authorized for #{channel}");
                    return;
                }

                // Broadcast to other sessions
                List<string> sessionIDs = new();
                if(channel != null) // Send to specific channel
                {
                    foreach (string key in _channels.Keys)
                    {
                        if (_channels[key] == channel && key != session.SessionID)
                        {
                            sessionIDs.Add(key);
                        }
                    }
                } 
                else // Send to general channel, that is everyone who hasn't connected to a specific one.
                {
                    var channelSessionIDs = _channels.Keys.ToList();
                    var allSessionIDs = _sessions.Keys.ToList();
                    allSessionIDs.RemoveAll(sessionID => channelSessionIDs.Contains(sessionID) || sessionID == session.SessionID);
                    sessionIDs.AddRange(allSessionIDs);

                }
                _server.SendMessageToGroup(sessionIDs.ToArray(), message);
            };
        }

        public void StartOrRestartServer(int port) {
            _server.Start(port);
        }

        private string GetCommandValue(string command, string message, bool toLowerCase = false, bool filterAlphaNumerical = false)
        {
            if(message.StartsWith($"{PREFIX}{command}:"))
            {
                var arr = message.Substring(PREFIX.Length).Split(':');
                if(arr.Length == 2 && arr[0] == command)
                {
                    var value = arr[1].Trim();
                    if(value.Length > 0)
                    {
                        if (toLowerCase) value = value.ToLower();
                        if (filterAlphaNumerical) value = Regex.Replace(value, "[^a-zA-Z0-9]", "");
                    }
                    return value;
                }
            }
            return "";
        }
    }
}