# Planning Poker

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
