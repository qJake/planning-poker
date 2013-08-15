<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PokerServerManager._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en">
<head>
    <title>Planning Poker Server Manager</title>
    <link href="styles/style.css" rel="stylesheet" type="text/css" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" > 
</head>
<body>
    <form runat="server">

        <div id="header">
            <h1>Planning Poker Server Manager</h1>
        </div>
        <div id="content">
            <p>Welcome to the Planning Poker Server Manager. Here, you can create a new server instance or manage existing instances.</p>

            <p><asp:Label runat="server" ID="Message" /></p>

            <h2>Create New Server</h2>

            <div class="indent">
                <table>

                    <tr>
                        <td class="label">Game Name:</td>
                        <td>
                            <asp:TextBox runat="server" ID="ServerName" Columns="16" />            
                            <asp:RegularExpressionValidator runat="server" ID="revServerName" ControlToValidate="ServerName" ValidationExpression="^[a-zA-Z0-9.\-_]+$"
                                Text="Invalid server name." Display="Dynamic" CssClass="Validation" />
                            <asp:RequiredFieldValidator runat="server" ID="rfvServerName" ControlToValidate="ServerName"
                                Text="Server name is required." Display="Dynamic" CssClass="Validation" />
                        </td>
                    </tr>

                    <tr>
                        <td colspan="2" class="info">
                            <img src="images/info.png" /> A unique name for your server/game. Cannot contain spaces. This is the name shown in the game when players connect.
                        </td>
                    </tr>

                    <tr>
                        <td class="label">Port Number:</td>
                        <td>
                            <asp:TextBox runat="server" ID="PortNumber" Columns="2" />
                            <asp:RangeValidator runat="server" ID="rvPortNumber" ControlToValidate="PortNumber" MinimumValue="8000" MaximumValue="9000" Type="Integer"
                                Text="Invalid port number (8000-9000)." Display="Dynamic" CssClass="Validation" />
                            <asp:RequiredFieldValidator runat="server" ID="rfvPortNumber" ControlToValidate="PortNumber"
                                Text="Port number is required." Display="Dynamic" CssClass="Validation" />
                        </td>
                    </tr>

                    <tr>
                        <td colspan="2" class="info">
                            <img src="images/info.png" /> The server port number. Must be between 8000 and 9000. No two servers can have the same port.
                        </td>
                    </tr>

                    <tr>
                        <td class="label">Password:</td>
                        <td>
                            <asp:TextBox runat="server" ID="Password1" TextMode="Password" Columns="12" />
                            <asp:RequiredFieldValidator runat="server" ID="rfvPass1" ControlToValidate="Password1"
                                Text="Password is required." Display="Dynamic" CssClass="Validation" />
                            <asp:CompareValidator runat="server" ID="cvPasswords" ControlToValidate="Password1" ControlToCompare="Password2"
                                ValueToCompare="Text" Operator="Equal" Text="Passwords do not match." Display="Dynamic" CssClass="Validation" /> 
                            <asp:RegularExpressionValidator runat="server" ID="revPass" ControlToValidate="Password1" ValidationExpression="^[^ ]+$"
                                Text="Invalid password (no spaces)." Display="Dynamic" CssClass="Validation" />
                        </td>
                    </tr>

                    <tr>
                        <td class="label">Again:</td>
                        <td>
                            <asp:TextBox runat="server" ID="Password2" TextMode="Password" Columns="12" />
                            <asp:RequiredFieldValidator runat="server" ID="rfvPass2" ControlToValidate="Password2"
                                Text="Password is required." Display="Dynamic" CssClass="Validation" />
                            <asp:RegularExpressionValidator runat="server" ID="revPass2" ControlToValidate="Password2" ValidationExpression="^[^ ]+$"
                                Text="Invalid password (no spaces)." Display="Dynamic" CssClass="Validation" />
                        </td>
                    </tr>

                    <tr>
                        <td colspan="2" class="info">
                            <img src="images/info.png" /> The administrative password for the server. This lets you log in as the game admin.
                        </td>
                    </tr>

                    <tr>
                        <td colspan="2">
                            <asp:Button runat="server" ID="Create" Text="Create" CausesValidation="true" />
                        </td>
                    </tr>
                </table>
            </div>

            <h2>Existing Servers <asp:Button runat="server" ID="Refresh" Text="Refresh" CausesValidation="false" /></h2>

            <p><strong style="color: red;">Notice:</strong> Servers are shut down every night at midnight. If you need to keep a server running for longer than 24 hours, please contact the server administrator.</p>
            <p><em>Please be courteous - do not shut down a server unless you were the one that created it.</em></p>
            <p><strong>New!</strong> Users now choose which server they want to connect to from a list, instead of typing in the server/port. Ensure that your server is named appropriately so that your players know which server to connect to!</p>
            <div class="indent">
                <asp:ListView runat="server" ID="ServerContainer">
                    <ItemTemplate>
                        <p>
                            <asp:ImageButton runat="server" ID="DeleteServer" ToolTip="Shut down the server" ImageUrl="images/delete.png"
                                CssClass="imageButton" CommandArgument='<%# Eval("Name") %>' CausesValidation="false" />
                            <asp:Label runat="server" ID="ServerName" Text='<%# Eval("Name") %>' />
                            [<asp:Label runat="server" ID="ServerPort" Text='<%# Eval("Port") %>' />]
                        </p>
                    </ItemTemplate>
                    <EmptyDataTemplate>
                        <p>There are no servers currently running.</p>
                    </EmptyDataTemplate>
                </asp:ListView>
            </div>
        </div>

    </form>
</body>
</html>
