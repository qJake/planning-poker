/*
    Planning Poker
    Client-side Game Script
    http://github.com/qjake/planning-poker
*/

// Configure this to be wherever you put your poker server manager application at:
var PokerServerManagerPath = 'http://localhost/PokerServerManager/';

var socket = null;
var clientName = null;
var clientID = null;
var isTryingAdmin = false;
var isAdmin = false;
var isSpectator = false;
var clientListCache = null;
var serverList = null;

var half = /^(0?\.5)|(1\/2)|(½)$/;
var serverPattern = /^([A-Za-z0-9]+):(\d{1,5})$/;

$(function ()
{
    $('#loginStatus').hide();
    $('#disconnected').hide();
    $('#newRoundOverlay').hide();
    $('#cardChooserOverlay').hide();
    $('#nameError').hide();
    $('.admin').hide();

    // Spectator swap starts out disabled and is re-enabled upon connect.
    $('#swapSpectator').attr('disabled', 'disabled');

    registerStaticEvents();

    // Load server list
    beginLoadServerList();
    beginLoadUserName();
});

function registerStaticEvents()
{
    // Navigation warning
    window.onbeforeunload = function (e)
    {
        e = e || window.event;

        var msg = "Warning: If you leave this page, you will be logged out of the game session. You will need to log back in if you return.";

        if (e)
            e.returnValue = msg;

        return msg;
    };

    // "Enter" handlers - these just trigger a 'Click' event most of the time.
    $('#adminPass').keypress(function (e)
    {
        if (e.which == 13)
        {
            $('#submitAdminLogin').trigger('click');
        }
    });
    $('#clientName').keypress(function (e)
    {
        if (e.which == 13)
        {
            $('#submitLogin').trigger('click');
        }
    });
    $('#serverName').keypress(function (e)
    {
        if (e.which == 13)
        {
            $('#submitLogin').trigger('click');
        }
    });
    $('#roundTitle').keypress(function (e)
    {
        if (e.which == 13)
        {
            $('#newRound').trigger('click');
        }
    });

    $('#submitLogin').click(function ()
    {
        isTryingAdmin = false;
        clientLogin();
    });

    $('#submitAdminLogin').click(function ()
    {
        isTryingAdmin = true;

        if ($('#adminPass').val().length == 0)
        {
            alert("Admin password must not be empty. If you did not intend to log in as an admin, please click the first 'Login' button instead.");
            return;
        }

        clientLogin();
    });

    $('#showNewRoundPanel').click(function ()
    {
        var hasActivePlayers = false;
        $('#clientList p').each(function ()
        {
            if ($(this).html().indexOf('[S]') == -1)
            {
                hasActivePlayers = true;
            }
        });
        if (!hasActivePlayers)
        {
            alert("There are no active players, so a new round cannot begin at this time.");
            return;
        }

        $('#newRoundOverlay').show();
        window.setTimeout(function () { $('#roundTitle').focus(); }, 1);
    });

    $('#newRound').click(function ()
    {
        $('#newRoundOverlay').hide();

        if ($('#roundTitle').val().length == 0)
        {
            alert('Round title cannot be empty.');
            return;
        }

        // Send round begin
        send('NewRoundRequest', [$('#roundTitle').val()]);

        $('#roundTitle').val('');
    });

    $('#cancelNewRound').click(function ()
    {
        $('#roundTitle').val('');
        $('#newRoundOverlay').hide();
    });

    $('#adminInfoClose').click(function ()
    {
        $('#adminOverlay').hide();
    });

    $('#showCardOptions').click(function ()
    {
        send('GetCardList');
        $('#cardChooserOverlay').show();
    });

    $('#cancelCardset').click(function ()
    {
        $('#cardSet').val('');
        $('#cardChooserOverlay').hide();
    });
    $('#applyCardset').click(function ()
    {
        send('SetCards', [$('#cardSet').val()]);
    });
    $('#autoSort').change(function ()
    {
        if ($(this).is(':checked'))
        {
            send('SetSetting', ['AutoSort', '1']);
        }
        else
        {
            send('SetSetting', ['AutoSort', '0']);
        }
    });
    $('#spectator').change(function ()
    {
        if ($(this).is(':checked'))
        {
            isSpectator = true;
        }
        else
        {
            isSpectator = false;
        }
    });
    $('#swapSpectator').click(function ()
    {
        $('#swapSpectator').attr('disabled', 'disabled');
        send('SwapSpectator');
    });
    $('#refreshServerList').click(function ()
    {
        beginLoadServerList();
    });
}

function RegisterDynamicEvents()
{
    // We also want to re-hide any new Admin elements that were inserted.
    if(!isAdmin)
    {
        $('.admin').hide();
    }

    // Set the height of the left panel to be the height of the page or the height of the window, whichever is larger.
    $('.menu').height(Math.max($('html').height(), window.screen.height));

    $('.round .cardup').click(function ()
    {
        send("RegisterVote", [$(this).html()]);
    });

    $('#flipCards').click(function()
    {
       send("FlipCards");
    });

    $('#takeMajority').click(function()
    {
        send('TakeMajority');
    });

    $('#restartRound').click(function()
    {
        send('RestartRound');
    });

    $('#overrideEstimate').click(function()
    {
        if(half.test($('#estOverride').val()))
        {
            $('#estOverride').val('½');
        }
        send('FinalizeVote', [$('#estOverride').val()]);
    });

    $('#discardActiveRound').click(function()
    {
        send('DiscardActiveRound');
    });

    $('#undoVote').click(function ()
    {
        send('UndoVote');
    });

    $('#sortCards').click(function ()
    {
        $('#sortCards').attr('disabled', 'disabled');
        send('SortCards');
    });
}

function beginLoadUserName() {
    $.ajax({
        url: 'api/GetUserName.aspx',
        dataType: 'json',
        success: function (d) {
            debugger;
            if (d.userName !== null && d.userName !== "") {
                clientName = d.userName;
                $('#clientName').val(clientName);
                $("#clientName").prop('disabled', true);
            }
        },
        error: function (x, h, r) {
            
        }
    });
}

function beginLoadServerList()
{
    $('#serverList').hide();
    $('#serverListText').show().html('Loading...');
    $.ajax({
        url: PokerServerManagerPath + 'ServerList.aspx',
        dataType: 'json',
        success: function (d)
        {
            serverList = d;

            if (serverList.servers.length == 0)
            {
                $('#serverListText').html('<em>No servers running.</em>');
            }
            else
            {
                $('#serverListText').hide();
                $('#serverList').show().empty();
                var group = $('<optgroup label="Select a server:" />');

                for (var i = 0; i < serverList.servers.length; i++)
                {
                    group.append($('<option />').text(serverList.servers[i].Name).val(i));
                }
                $('#serverList').append($('<option />').val(-1)).append(group);
            }
        },
        error: function (x, h, r)
        {
            $('#serverListText').html('Unable to retrieve list. Refresh to try again.');
        }
    });
}

function clientLogin()
{
    if ($('#clientName').val().length < 2)
    {
        alert("Name is too short.");
        return;
    }

    everConnected = false;

    if (typeof WebSocket === 'undefined')
    {
        alert("Sorry, it appears this browser is not supported. Please try another browser.\r\n\r\nWe recommend Chrome, Firefox, or Internet Explorer 10+.");
        return;
    }

    var serverListIndex = $('#serverList').val();
    if (serverListIndex == -1)
    {
        alert("Please select a server to connect to.");
        return;
    }
    socket = new WebSocket('ws://' + serverList.machineName + ':' + serverList.servers[serverListIndex].Port + '/');

    setupSocket();

    $('#loginForm').hide();
    $('#disconnected').hide();
    $('#loginStatus').show();

    clientName = $('#clientName').val();
}

function setupSocket()
{
    socket.onopen = function ()
    {
        send("RegisterClient", [clientName, ($('#spectator').is(":checked") ? 1 : 0)]);
        if (isTryingAdmin)
        {
            send("RegisterAdmin", [$('#adminPass').val()]);
        }
    };
    socket.onmessage = function (event)
    {
        processIncoming(JSON.parse(event.data));
    };
    socket.onclose = function ()
    {
        $('#overlay').show();
        $('#loginForm').show();
        $('#loginStatus').hide();
        $('#newRoundOverlay').hide();
        if (everConnected)
        {
            $('#disconnected').html('The server has terminated the connection.');
        }
        else
        {
            $('#disconnected').html('Unable to connect to the server.');
        }
        $('#disconnected').show();
        everConnected = false;
    };
}

function processIncoming(msgData)
{
    // console.info(msgData);

    if (typeof(msgData.Error) !== 'undefined' && !msgData.Error)
    {
        switch (msgData.Command)
        {
            // Handle success commands here.
            case 'RegisterClient':
                clientID = msgData.ClientID;
                $('#overlay').hide();
                everConnected = true;
                if (isSpectator)
                {
                    $('#swapSpectatorContent').html('I want to play');
                    $('#specIcon').removeClass('icon-pause').addClass('icon-play');
                }
                else
                {
                    $('#swapSpectatorContent').html('I want to spectate');
                    $('#specIcon').removeClass('icon-play').addClass('icon-pause');
                }
                $('#swapSpectator').removeAttr('disabled');
                break;
            case 'RegisterAdmin':
                registerAsAdmin();
                break;
            case 'GameState':
                updateView(msgData.Data);
                break;
            case 'ClientList':
                updateClientList(msgData.Clients);
                break;
            case 'GetCardList':
                updateCardList(msgData.CardList);
                break;
            case 'SetCards':
                $('#cardChooserOverlay').hide();
                break;
            case 'SortCards':
                $('#sortCards').attr('disabled', 'disabled');
                break;
            case 'LockUndo':
                $('#undoVote').attr('disabled', 'disabled');
                break;
            case 'SpectatorSwap':
                isSpectator = msgData.IsSpectator;
                if (msgData.IsSpectator)
                {
                    $('#swapSpectatorContent').html('I want to play');
                    $('#specIcon').removeClass('icon-pause').addClass('icon-play');
                }
                else
                {
                    $('#swapSpectatorContent').html('I want to spectate');
                    $('#specIcon').removeClass('icon-play').addClass('icon-pause');
                }
                $('#swapSpectator').removeAttr('disabled');
                break;
        }

    }
    else
    {
        // Handle error
        switch (msgData.Source)
        {
            case 'RegisterAdmin':
                alert("Error: " + msgData.ErrorMessage + '\r\n\r\n' + 'You have been logged in as a standard user. To try admin login again, refresh the page.');
                break;
            case 'SortCards':
                $('#sortCards').removeAttr('disabled'); // SortCards failed, so re-enable the button.
                alert(msgData.ErrorMessage);
                break;
            default:
                alert(msgData.ErrorMessage);
                break;
        }
    }
}

function updateCardList(data)
{
    $('#cardSet').val(data);
}

/*
Sample client data:
[
    {
        ID: <guid>,
        Name: 'Bob',
        IsSpectator: false,
        IsAdmin: false
    },
    {
        ID: <guid>,
        Name: 'Adam',
        IsSpectator: true,
        IsAdmin: true
    }
]
*/
function updateClientList(data)
{
    // Update the cache first
    clientListCache = data;

    $('#clientList').empty();

    data.forEach(function (e)
    {
        var thisClient = $('<p class="client">' + e.Name + '</p>');

        if (e.ID == clientID)
        {
            // "You" color.
            thisClient.css('color', '#2DA1FA');
        }
        if (e.IsSpectator)
        {
            // Spectator color / adornment.
            thisClient.html(thisClient.html() + " [S]");
        }
        if (e.IsAdmin)
        {
            // Admin color / adornment.
            thisClient.html(thisClient.html() + " <span style=\"color: red;\">[A]</span>");
        }

        $('#clientList').append(thisClient);
    });
}

function registerAsAdmin()
{
    isAdmin = true;
    window.setTimeout(function () { $('#adminInfoClose').trigger('focus'); }, 1);
    $('.admin').show();
}

/*
Sample round data:
[
    {
        Votes: [
            {
                ClientID: <guid>,
                ClientName: 'Bob',
                Vote: '1'
            },
            {
                ClientID: <guid>,
                ClientName: 'Adam',
                Vote: '3'
            }
        ],
        Title: Story 1,
        Flipped: true,
        Decision: ''
    },
    {
        Votes: [],
        Title: Story 2,
        Flipped: false,
        Decision: '1'
    }
]
*/
function updateView(data)
{
    var rounds = data.RoundData;
    var cardDefinition = data.CardSet;
    var cards = cardDefinition.split(',');

    $('#rounds').empty();

    var i = 0; // Round counter

    // If there aren't any rounds, print a friendly message about it.
    if (rounds.length <= 0)
    {
        $('#rounds').append('<p>No rounds have been started yet. <span class="admin"><span style="font-weight: bold;">Admin:</span> Start a new round by clicking New Round in the menu to the right.</span></p>');
    }
    else
    {
        // Round loop
        rounds.forEach(function (e)
        {
            // Increment counter
            i++;

            // Add the title
            $('#rounds').append('<h3>' + e.Title + '</h3>');

            if (e.Decision.length > 0)
            {
                // This round has been decided already.
                $('#rounds').append($('<div></div>').addClass('decision').html('Estimate: ' + e.Decision))
                            .append('<div class="separator"><hr /></div>');
            }
            else
            {
                // This round is still voting
                var roundContainer = $('<div></div>');
                roundContainer.addClass('round');
                roundContainer.prop('id', 'round-' + i);

                var votesContainer = $('<div></div>');
                votesContainer.addClass('votes');
                votesContainer.prop('id', 'votes-' + i);

                // Vote loop, displays cards people have submitted, either up or down.
                e.Votes.forEach(function (v)
                {
                    var voteView = $('<div></div>').addClass('voteContainer');

                    // Show card value if this card belongs to the user or the cards are flipped.
                    if (e.Flipped || v.ClientID == clientID)
                    {
                        voteView.append('<div class="cardup">' + v.VoteValue + '</div>');
                    }
                    else
                    {
                        voteView.append('<div class="carddown">&nbsp;</div>');
                    }

                    // Add the owner's name to the bottom.
                    voteView.append('<div class="cardName">' + v.ClientName + '</div>');

                    votesContainer.append(voteView);
                });

                // Fill in the rest of the slots with people that haven't voted yet (if we're still voting, that is).
                // We don't know who they are, but we know how many from the client list.
                if (!e.Flipped)
                {
                    var clientCount = 0;
                    $('#clientList p').each(function ()
                    {
                        if ($(this).html().indexOf('[S]') == -1)
                        {
                            clientCount++;
                        }
                    });

                    for (var k = 0; k < clientCount - (e.Votes.length) ; k++)
                    {
                        voteView = $('<div></div>').addClass('voteContainer');
                        voteView.append('<div class="cardup cardoutline cardUndecided">?</div>');
                        voteView.append('<div class="cardName cardUndecided">Undecided</div>');
                        votesContainer.append(voteView);
                    }
                }

                $('#rounds').append(votesContainer);

                // Only add voting cards if a decision hasn't been made, the cards haven't been flipped, 
                // this user hasn't voted yet, and this user isn't a spectator.
                if (e.Decision.length <= 0 && !e.Flipped && !hasUserVoted(e.Votes, clientID) && !isSpectator)
                {
                    // Card loop
                    cards.forEach(function (c)
                    {
                        roundContainer.append('<div class="cardup">' + c + '</div>');
                    });
                    $('#rounds').append(roundContainer);
                }

                // Add the admin functions for this round
                // Admin panel looks like this:
                // If voting is still in progress: <Flip Cards>
                // If the cards are flipped: <Take Majority> <Restart Round> Override Estimate: [      ] <Apply>

                var optionContainer = $('<div />').addClass('options');
                var hasOptions = false;

                // If we haven't flipped yet, and this client has voted already, and we're not a spectator, add the option to undo your vote.
                if (!e.Flipped && hasUserVoted(e.Votes, clientID) && !isSpectator)
                {
                    optionContainer.append('<button id="undoVote"><i class="icon-download-alt icon-large"></i> Undo Vote</button>');
                    if (e.Votes.length == clientListCache.length)
                    {
                        optionContainer.find('#undoVote').attr('disabled', 'disabled');
                    }
                    hasOptions = true;
                }

                var adminContainer = $('<div />').addClass('admin').css('display', 'inline-block');

                // We haven't flipped yet, so add the option to flip the cards.
                if (!e.Flipped)
                {
                    adminContainer.append('<button id="flipCards"><i class="icon-retweet icon-large"></i> Flip Cards</button>');
                }
                else
                {
                    // Cards are flipped, present finalization options.
                    adminContainer.append('<button id="sortCards"><i class="icon-signal icon-large"></i> Sort Cards</button> ' +
                                            '<button id="takeMajority"><i class="icon-tasks icon-large"></i> Take Majority</button> ' +
                                            'Enter Estimate: <input type="text" size="8" id="estOverride"></i> <button id="overrideEstimate"><i class="icon-chevron-right icon-large"></i> Go</button> ' +
                                            '<button id="restartRound"><i class="icon-refresh icon-large"></i> Restart Round</button>');

                    // If auto-sort is on, grey out the sort button because it's already been sorted.
                    // Actual auto-sort operation is handled server-side.
                    if ($('#autoSort').is(':checked'))
                    {
                        adminContainer.find('#sortCards').attr('disabled', 'disabled');
                    }
                }
                adminContainer.append('<button id="discardActiveRound"><i class="icon-trash icon-large"></i> Discard Round</button>');

                optionContainer.append(adminContainer);

                if (hasOptions || isAdmin)
                {
                    $('#rounds').append(optionContainer);
                }
            }
        });

        // Append a spacer to the bottom so that recent games aren't stuck at the bottom of the window.
        $('#rounds').append('<div class="bottomSpacer"> </div>');
    }

    // Finally, re-hook up all the event listeners.
    RegisterDynamicEvents();
}

function hasUserVoted(votes, clientID)
{
    for (var i = 0; i < votes.length; i++)
    {
        if (votes[i].ClientID == clientID) return true;
    }
    return false;
}

function isNumber(n)
{
  return !isNaN(parseFloat(n)) && isFinite(n);
}

// JSON-serializes an object (this is just a quick wrapper to make other source easier to read).
function send(name, args)
{
    if (typeof (name) !== 'string' || name === null)
    {
        throw 'Name was not a string (in function send()).';
    }
    if (typeof (args) === 'undefined' || args === null || typeof (args) !== 'object')
    {
        args = [];
    }
    socket.send(JSON.stringify({ MethodName: name, MethodArguments: args }));
}