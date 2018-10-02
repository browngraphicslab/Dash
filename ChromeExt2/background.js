var ENDPOINT = "ws://127.0.0.1:12345/dash/chrome";
var ws = null;
var screenHeight = 0;
var currentUrl = "";
var isActive = false;

function send(socket, prefix, message) {
    var s = JSON.stringify([JSON.stringify(message)]);
    if (prefix !== null) {
        s = prefix + ":" + s;
    }
    socket.send(s);
}

function isPdf(url) {
    return url.endsWith(".pdf");
}

// called anytime a tab is added/changed to update the browser window's height
function updateSize(url) {
    var targetHeight = 0;
    if (isPdf(url)) {
        targetHeight = 0;
    } else {
        targetHeight = screenHeight;
    }

    chrome.windows.getCurrent(function (e) {
        chrome.windows.update(e.id, { height: targetHeight }, function (win) {
            if (isPdf(url)) {
                send(ws, "collapse",
                    {
                        "$type": "Dash.CollapseRequest, Dash",
                        "expanded": false,
                        "url": url
                    });
            } else {
                send(ws, "expand",
                    {
                        "$type": "Dash.CollapseRequest, Dash",
                        "expanded": true,
                        "url": url
            });
            }
        });
    });
}

function sendStatus() {
    if (ws == undefined || ws.readyState != ws.OPEN) {
        chrome.runtime.sendMessage("status:error");
    } else if (isActive) {
        chrome.runtime.sendMessage("status:connected");
    } else {
        chrome.runtime.sendMessage("status:disconnected");
    }
}

// called when the button in the popup is clicked
chrome.runtime.onMessage.addListener(
    function (request, sender, sendResponse) {
        

        if (request === "requestStatus") {
            sendStatus();
            return;
        }

        if (request === "toggleActive") {

            chrome.windows.getCurrent(function (e) {
                screenHeight = e.height;
            });

            if (!isActive) {
                if (ws == undefined || ws.readyState !== ws.OPEN) {
                    ws = new WebSocket(ENDPOINT);
                    ws.onopen = function () {
                        isActive = true;
                        sendStatus();
                        send(ws, "activate",
                            {
                                "$type": "Dash.ActivateRequest, Dash",
                                "activated": true
                            });
                    }
                    ws.onerror = function () {
                        sendStatus();
                    }
                } else {
                    isActive = true;
                    sendStatus();
                    send(ws, "activate",
                        {
                            "$type": "Dash.ActivateRequest, Dash",
                            "activated": true
                        });
                }
            } else {
                isActive = !isActive;
                sendStatus();
                if (isActive) {
                    send(ws,
                        "activate",
                        {
                            "$type": "Dash.ActivateRequest, Dash",
                            "activated": true
                        });
                } else {
                    send(ws, "deactivate",
                        {
                            "$type": "Dash.ActivateRequest, Dash",
                            "activated": false
                        });
                }
            }
        }
    }
);

// called when tabs are switched
chrome.tabs.onUpdated.addListener(function (tabId, info) {

    if (!isActive) {
        return;
    }

    if (info.url != undefined) {
        currentUrl = info.url;
        send(ws, null,
            {
                "$type": "Dash.UrlChangedRequest, Dash",
                "url": currentUrl
            });
        updateSize(currentUrl);
    }
});

// called when tabs are switched
chrome.tabs.onActivated.addListener(function (info1) {

    if (!isActive) {
        return;
    }

    chrome.tabs.get(info1.tabId, function (info2) {
        currentUrl = info2.url;
        send(ws, null,
            {
                "$type": "Dash.UrlChangedRequest, Dash",
                "url": currentUrl
            });
        updateSize(currentUrl);
    });
});