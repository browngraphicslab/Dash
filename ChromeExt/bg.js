console.log("background launched.")

$(window).bind('hashchange', function () {
    console.log(window.url)
});

var start = function() {
    useSocket = true;
    var socket;

    var socketOpen = false;
    var messagesToSend = []
    var sending = false;

    var pollSend = function() {
        if (socketOpen === true && messagesToSend.length > 0 && !sending) {
            sending = true;
            var array = JSON.stringify(messagesToSend)
            messagesToSend.length = 0;
            socket.send(array)
            sending = false
        }
    }

    setInterval(pollSend, 50);

    var sendFunction = function(messageObject) {
        //console.log(socket)
        //messagesToSend.push(messageObject)
        messagesToSend.push(JSON.stringify(messageObject))
    }

    var manager = new tabManager(sendFunction);
    var handler = new requestHandler(manager);

    if (useSocket) {
        if ("WebSocket" in window) {
            socket = new WebSocket("ws://dashchromewebapp.azurewebsites.net/api/values");
        } else {
            console.log("WebSocket is NOT supported by your Browser!");
        }

        socket.onopen = function() {
            console.log("Connection Opened");
            socket.send("browser:123")
            socketOpen = true;
        }

        socket.onclose = function () {
            console.log("close occurred... starting again")
            start();
        }

        socket.onerror = function () {
            console.log("error occurred... starting again")
            start();
        }

        socket.onmessage = function(msg) {
            console.log(msg);
            handler.handle(msg.data);
        }
    }

    tabs_initialized = {}
    tabs_active = {}

    function guid() {
        function s4() {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        }

        return s4() +s4() + s4() +  s4() + s4() +s4() + s4() +s4() + s4() +s4();
    }
}
start();

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