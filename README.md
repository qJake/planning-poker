# Planning Poker

**Planning Poker** is an open-source browser-based .NET/C# planning poker card game app. The underlying communication protocol used between the client and the server is WebSockets.

### What is planning poker?

Planning poker is commonly used in agile development to obtain arbitrary numeric estimates from developers. [Planning poker has a Wikipedia page](http://en.wikipedia.org/wiki/Planning_poker) if you'd like to learn more.

### Project Overview and Features

This project is a client and server (and server manager) for a WebSocket-based planning poker application.

#### Client

The client is a web-based Javascript/WebSocket client that communicates using JSON messages.

#### Server

The server is a .NET console application that uses the [statianzo/Fleck](https://github.com/statianzo/Fleck) library for hosting the WebSocket server, and [Json.NET](http://james.newtonking.com/pages/json-net.aspx) for parsing the JSON messages.

#### Server Manager

The server manager is able to spin up instances of servers automatically using a clean web interface, allowing anyone with access to the page the ability to create their own planning poker server that will appear in the server list to each client. The server manager communicates with the individual servers using [named pipes](http://en.wikipedia.org/wiki/Named_pipe).

## Screenshots

### Connecting

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp1.png)

### Playing as an admin

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp2.png)

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp3.png)

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp6.png)

### Playing as a standard user

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp4.png)

![Planning Poker Screenshot](https://raw.github.com/qJake/planning-poker/master/readme-resources/pp5.png)

## Caveats

Planning Poker uses WebSockets as its underlying communication protocol. Specifically, it must be able to connect over the `ws://` protocol between ports 8000 and 9000.

Currently, Planning Poker is not set up to run in SSL-only environments. The game client must be loaded over HTTP, so that the WebSocket connection can be made. This also allows multiple game sessions to exist at the same time (if SSL were supported, only one game would be able to run, since the `wss://` protocol must connect over port 443 exclusively).

There are no plans to support HTTPS because of the way the game servers run on different ports.

## Setup

You will need:

* An IIS server
* Chrome, Firefox, Safari, or IE10+ (due to the websocket requirement)

### Setup Instructions

1. Clone the code.
2. Open it with Visual Studio 2010 / 2012.
3. Make sure the solution builds.

#### Client Setup

1. Open the file `PokerWebClient/CardClient.js`
2. Near the top you will see this code:

    ```js
    // Configure this to be wherever you put your poker server manager application at:
    var PokerServerManagerPath = '/PokerServerManager/';
    ```

3. Change this value to the path that you will be using for your poker server manager. *(This has only been tested using the same webserver for both the client and the poker server manager, putting in a full http:// path here is untested but may work.)* Save your changes.
4. Publish the PokerWebClient project to a folder or virtual app in IIS. There is only HTML/JS/CSS in the client so there is no need to set anything up in IIS beyond the basics.
5. Browse to where you published the website to make sure it loads correctly in your browser.

#### Server Setup

1. Build the PokerServer project (you'll probably want to do this in Release mode for faster code).
2. Navigate to /bin/Release (or whatever the active configuration is).
3. Copy PokerServer.exe and the two .dll files (Fleck and Newtonsoft.Json) and place them somewhere on your server where they can be run. They do not have to (and should not) be accessible from the web.

#### Server Manager Setup

1. Open PokerServerManager/web.config
2. You should see these three configuration values:

    ```xml
    <!-- Must be changed to the folder location of the EXE of the server executable. -->
    <add key="SocketServerLocation" value="C:\Path\To\The\Game\" />
        
    <!-- Name of the EXE, without '.exe' -->
    <add key="SocketServerName" value="PokerServer" />
        
    <!-- Name of the computer / host that the servers get run on. This gets sent to the client, and they connect via WebSockets using ws://<MachineName>/.  -->
    <add key="MachineName" value="MyPokerGameServer" />
    ```

3. **SocketServerLocation** is the name of the folder / path where the web socket server can be found.
4. **SocketServerName** is the name of the EXE without '.exe'. This is almost always "PokerServer" unless you rename the exe or change the assembly name.
5. **MachineName** is the name of the computer where the game servers are going to be running. The clients will use this value when trying to open a websocket connection in order to connect to the game server (i.e. `ws://MyPokerGameServer/`).

## Contributing

Fork it, make improvements, and send me a pull request. If I like the changes, I'll accept the code. Simple as that!
