# WSRelay
This application is a simple Websocket relay server, runs on Windows 10 (at least) with .NET 6.
* It lets clients connect and communicate with each other through channels with optional password protection.
* It is meant for internal network use to allow Websocket enabled clients to interconnect, and is not secured for public deployment.

## Usage
1. Run the application, it automatically minimizes to the system tray.
2. Click the system tray icon (hash-tag-like smiley) to open the application window.
3. Change the port in the application window if need be, default is `7788`.
4. Launch a client and connect to `ws://localhost:7788` or with your custom port. If you are running this on another machine on the network, use that IP.
5. Use the command messages in the next section to connect to a channel and optionally authorize, or skip that to send message to the general channel you are automatically in from the start.

## Commands
Commands are sent as raw text using reserved prefixed (`:::`) keywords, there are only two, listed below.
### Channel
You are automatically connected to a non-protected general channel upon first connection, to narrow down communication the client needs to connect to a channel.  
Any channel name used will filter down to only lower-case alpha-numerical characters.  
A channel is automatically allocated when connecting to it.
```
:::CHANNEL:yourchannel
```
### Password
If any client has submitted a password, other clients needs to do the same.  
When doing this and no password has been set yet, you set the password and others will have to authenticate instead.  
It is basically a system where the first one to set a password gets to dictate what it is.
```
:::PASSWORD:yourpassword
```
## Responses
Sending command messages to the server will net you responses with this format, it is a good idea to listen for these in your client:
```
:::SUCCESS/ERROR:CODE:MESSAGE
```
A few examples would be:
```
:::SUCCESS:10:Connected to #a_channel
:::ERROR:51:Not authorized for #a_channel
```
A general rule is that any code below 50 means things are OK, anything 50 or above is an error.
## Broadcasting
After you have joined a channel and optionally authorized, just send any text to the server that is not matched as a command: plain text, JSON, base64 encoded binary blobs, etc.

As mentioned the only special cases are the commands, which you can see in the commands section above, they are consumed by the server and will not be broadcast.  
Anything else is immediately re-distributed to any other clients also connected to the same channel.
