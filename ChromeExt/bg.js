console.log("background launched.")

$(window).bind('hashchange', function () {
    console.log(window.url)
});
/*
var getLocalIPs = function (callback) {
    try {
        var ips = [];

        var RTCPeerConnection = window.RTCPeerConnection ||
            window.webkitRTCPeerConnection ||
            window.mozRTCPeerConnection;

        var pc = new RTCPeerConnection({
            // Don't specify any stun/turn servers, otherwise you will
            // also find your public IP addresses.
            iceServers: []
        });
        // Add a media line, this is needed to activate candidate gathering.
        pc.createDataChannel('');

        // onicecandidate is triggered whenever a candidate has been found.
        pc.onicecandidate = function (e) {
            try {
                if (!e.candidate) { // Candidate gathering completed.
                    pc.close();
                    callback(ips);
                    return;
                }
                var ip = /^candidate:.+ (\S+) \d+ typ/.exec(e.candidate.candidate)[1];
                if (ips.indexOf(ip) == -1) { // avoid duplicate entries (tcp/udp)
                    ips.push(ip);
                }
            } catch (ee) {
                callback(["", "123"]);
            }
        };
        pc.createOffer(function(sdp) {
                pc.setLocalDescription(sdp);
            },
            function onerror() {});
    } catch (e) {
        callback(["", "123"]);
    }
}*/

var start = function () {
    document.body.style.backgroundColor = "red";

    useSocket = true;
    var socket = null;

    var messagesToSend = [];
    var sending = false;

    var pollSend = function () {
        //TODO Can we just call send and have socket buffer it?
        if (socket.readyState === WebSocket.OPEN && messagesToSend.length > 0 && !sending) {
            sending = true;
            var array = JSON.stringify(messagesToSend);
            messagesToSend.length = 0;
            console.log("sending message array with length: " + array.length);
            socket.send(array);
            sending = false;
        }
    }

    setInterval(pollSend, 50);

    //this function is used in tabManager whenever it gets a result
    var sendFunction = function (messageObject) {
        //console.log(socket)
        //messagesToSend.push(messageObject)
        messagesToSend.push(JSON.stringify(messageObject));
    }

    chrome.runtime.onMessage.addListener(function(request) {
        if (request.type === "sendRequest") {
            sendFunction(request.data);
        }
    });

    var manager = new tabManager(sendFunction);
    var handler = new requestHandler(manager);

    function connect() {
        if (useSocket) {
            socket = new WebSocket("ws://127.0.0.1:12345/dash/chrome");

            socket.onopen = function () {
                console.log("Connection Opened");
            }

            socket.onclose = function () {
                console.log("close occurred... starting again");
            }

            socket.onerror = function () {
                console.log("error occurred... starting again");
            }

            socket.onmessage = function (msg) {
                console.log("Received message from interop");
                handler.handle(msg.data);
            }
        }
    }


    if ("WebSocket" in window) {
        setInterval(function () {
            if (socket == null || socket.readyState === WebSocket.CLOSED) {
                connect();
            }
        },
            200);
        connect();
    } else {
        console.log("WebSocket is NOT supported by your Browser!");
    }

    tabs_initialized = {}
    tabs_active = {}

    function guid() {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }

        return s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4() + s4();
    }
}
document.body.style.backgroundColor = "red";
start();
/*
getLocalIPs(function (ips) {
    console.log(ips);
    start(ips[1]);
});*/


/*
///will call the callback when it returns. 
///This will return a bool for success
///Pass in the document object
function httpPostDocRequest(doc, callback) {
    var url = 'http://doc-eng.azurewebsites.net/api/UploadNewDoc/';
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
            callback(xmlHttp.responseText);
        }
    }

    xmlHttp.open("POST", url, true); // true for asynchronous 
    xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
    xmlHttp.send(JSON.stringify(doc));
}

///will call the callback when it returns. 
///This will return a bool for success
///Pass in the tag document request object
function httpAddTagRequest(tagRequest, callback) {
    var url = 'http://doc-eng.azurewebsites.net/api/AddTag/';
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
            callback(xmlHttp.responseText);
        }
    }

    xmlHttp.open("POST", url, true); // true for asynchronous 
    xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
    xmlHttp.send(JSON.stringify(tagRequest));
}

var successFunc = function(b) {
    console.log("request success:"+b)
}

chrome.extension.onMessage.addListener(function(request, sender, response) {
    
    // activates the plugin when key 'n' is pressed
    if (request.msg == "activate") {
        activate();
        return;
    }
    
    if (request.msg == "getSelectionId") {
        response( {id: guid()} );
        return;
    }

    if (request.msg == "newdoc") {
        request.type = "doc"
        //TODO: Trent code goes here.
        console.log("NEW DOC")
        console.log(request)
        httpPostDocRequest(request, successFunc);
        return;
    }

    if (request.msg == "selection") {
        console.log("got selection");

        
        if (request.type == "website") {
             chrome.tabs.captureVisibleTab(
                null,
                {},
                function(imgData)
                {
                    request.data = imgData.substring(imgData.indexOf(',')+1);

                    console.log(request)
                    httpPostDocRequest(request, successFunc);
                }
            );
        } else {
            console.log(request);
            httpPostDocRequest(request, successFunc);
        }
    }

    if (request.msg == "tag") {
        console.log("got tag");
        console.log(request);
        request.selectionId = request.selcetionId;
        httpAddTagRequest(request, successFunc);
    }

    if (request.msg == "field") {
        request.selectionId = request.selcetionId;
        request.type = "field"
        request.data = JSON.stringify(request.data)
        console.log("got field");
        console.log(request)
        httpPostDocRequest(request, successFunc);
        //TODO: Trent code goes here.
    }
});


chrome.tabs.onUpdated.addListener(function(tabId,changeInfo,tab){
    if (changeInfo.status != "complete")
        return;

    tabs_initialized = {}
    tabs_active = {}
    chrome.browserAction.setIcon({ path: { 19: "icon.png", 38: "icon_active.png" } });
    // add key detector.
});


chrome.browserAction.onClicked.addListener( function() {    
    console.log("click");
    activate();
});

function activate() {
    chrome.tabs.getSelected(null, function(tab) {

         if (tabs_initialized[tab.id] != true) {
            
            initTab(tab);
         }
        console.log(tabs_active[tab.id])
        if (tabs_active[tab.id] == false || tabs_active[tab.id] == null) {
            tabs_active[tab.id] = true;
            chrome.browserAction.setIcon({ path: { 19: "icon_active.png", 38: "icon.png" } });
            chrome.tabs.sendMessage(tab.id, {msg: "activate"}, function(response) { });
        } else {
            tabs_active[tab.id] = false;
            chrome.browserAction.setIcon({ path: { 19: "icon.png", 38: "icon_active.png" } });
            chrome.tabs.sendMessage(tab.id, {msg: "deactivate"}, function(response) { });
        }
        
    });
}

function initTab(tab) {
    console.log("init tab");
        tabs_initialized[tab.id] = true;    
        chrome.tabs.executeScript(tab.id, { file: "jquery.js" }, function(result) {
            if (chrome.runtime.lastError) {
                console.log("error in loading jquery");
                console.log(chrome.runtime.lastError);
                return;
            } else {
                console.log("jquery injected.");
            }
        });
    

    
    var menuMain = $.get(chrome.extension.getURL("menu_main.html"));
    var menuMainDoc = $.get(chrome.extension.getURL("menu_main_document.html"));
    var menuTags = $.get(chrome.extension.getURL("menu_tags.html"));
    var menuInlineSave = $.get(chrome.extension.getURL("menu_inline_save.html"));
    var menuInlineField = $.get(chrome.extension.getURL("menu_inline_field.html"));
    var css = $.get(chrome.extension.getURL("style.css"));

    $.when(menuMain, menuMainDoc, menuTags, menuInlineSave, menuInlineField, css).done(function() {

        console.log("loaded all the html.")
        chrome.tabs.executeScript(tab.id, { file: "selections.js" }, function(result) {
            if (chrome.runtime.lastError) {
                console.log("error in loading jquery");
                return;
            } else {
                console.log("selections.js injected.")
                  chrome.tabs.sendMessage(tab.id, { msg: "init", args: {  menuMain: menuMain.responseText.replace("[logo.png]", chrome.runtime.getURL("logo.png") ),
                                                                        menuMainDoc: menuMainDoc.responseText,
                                                                        menuTags: menuTags.responseText,
                                                                        menuInlineField: menuInlineField.responseText,
                                                                        menuInlineSave: menuInlineSave.responseText,
                                                                        css: css.responseText.replace("[logo.png]", chrome.runtime.getURL("logo.png")).replace("[close-icon.svg]", chrome.runtime.getURL("close-icon.svg")) }}, function() {
                    tabs_active[tab.id] = true;
                    chrome.browserAction.setIcon({ path: { 19: "icon_active.png", 38: "icon.png" } });
                    chrome.tabs.sendMessage(tab.id, {msg: "activate"}, function(response) { });
                });
              
            }
        });
    });                            
}*/