# WSRelay
This application is a simple Websocket relay server.
* It lets clients connect and communicate with each other through channels with optional password protection.
* It is meant for internal network use to allow Websocket enabled clients to interconnect, and is not secured for public deployment.

## Usage
1. Run the application.
2. Change the port in the application window if need be, default is 7788.
3. Launch a client and connect to `ws://localhost:7788` if you are using it on your local machine, else use the IP of the machine it's running on.
4. Use the command messages in the next section to enable broadcasting.

## Commands
Commands are sent as raw text using reserved keywords, there are only two, listed below.
### Channel
To be able to send and receive messages the client needs to connect to a channel.  
Any channel name used will filter down to only lower-case alpha-numerical characters.  
A channel is automatically allocated when connecting.
```
CHANNEL:yourchannel
```
### Password
If the channel should be secured, you can set a password.  
If a password already has been set by someone else, you are instead authenticating.
```
PASSWORD:yourpassword
```
## Responses
Sending command messages to the server will net you responses with this format, it is a good idea to listen for these in your client:
```
SUCCESS/ERROR:CODE:MESSAGE
```
A few examples would be:
```
SUCCESS:10:Connected to #a_channel
ERROR:51:Not authorized for #a_channel
```
A general rule is that any code below 50 means things are OK, anything 50 or above is an error.
## Broadcasting
It is as easy as just sending any text to the server, be it JSON, base64 encoded binary blobs, etc.  
Encoding the contents does help differentiating it from messages from the sever, but is optional.

The only special cases are the commands you see above, they consumed by the server and will not be broadcast.  
Anything else is simply distributed to any other clients also connected to the channel.
