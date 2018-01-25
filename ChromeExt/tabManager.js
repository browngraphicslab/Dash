
function tabManager(sendRequestFunction) {
    var tabs = {}
    var activeTabId = 0;
    var prevActiveTabScroll = {}
    var awaitingUpdateTimers = {}

    /*
    var updateScroll = function(tabId, scroll) {
        var requestBody = {
            "$type": "Dash.UpdateScrollBrowserRequest, Dash",
            "tabId": tabId,
            "scroll": scroll
        }
        sendRequestFunction(requestBody)
    }*/

    var updateTab = function (tabId) {
        var update = function (tab) {
            var finalUpdate = function (result) {
                if (tab != null) {
                    var requestBody = {
                        "$type": "Dash.UpdateTabBrowserRequest, Dash",
                        "tabId": tab.id,
                        "current": tab.active,
                        "url": tab.url,
                        "title": tab.title,
                        "index": tab.index,
                        "scroll" : result[tabId.toString()]
                    }
                    sendRequestFunction(requestBody);
                }
                else {
                    console.log("error: tab was null.  ID: " + tabId);
                }
            }
            chrome.storage.local.get(tabId.toString(), finalUpdate);

        }
        chrome.tabs.get(tabId, update);
    }


    var sendTabScreenshot = function (tabId) {
        console.log("taking screenshot");
        var imgUpdate = function (imgResult) {
            console.log(imgResult.substring(0, 50));
            var requestBody = {
                "$type": "Dash.SetTabImageBrowserRequest, Dash",
                "tabId": tabId,
                "data" : imgResult
            }
            sendRequestFunction(requestBody);
            console.log("sent screenshot");
        }
        chrome.tabs.captureVisibleTab(imgUpdate);
    }

    var updateScrollFromId = function (tabId) {
        if (!(tabId.toString() in prevActiveTabScroll)) {
            prevActiveTabScroll[tabId.toString()] = 0;
        }
        chrome.tabs.executeScript(tabId, { code: "var scroll = document.documentElement.scrollTop; chrome.storage.local.set({ " + tabId + ": scroll }, function (result) {});" }, function () {
            chrome.storage.local.get(tabId.toString(), function (result) {
                var scroll = result[tabId.toString()];
                if (prevActiveTabScroll[tabId.toString()] !== scroll) {
                    updateTab(tabId);
                }
                prevActiveTabScroll[tabId.toString()] = scroll;
            });
        });
    }

    var activeTabPoll = function () {
        if (activeTabId == 0) {
            setTimeout(activeTabPoll, 100);
            return;
        }
        /*
        var script = "console.log('here'); var doc = document.documentElement; var top = (window.pageYOffset || doc.scrollTop) - (doc.clientTop || 0); console.log(top); return top;";
        chrome.tabs.executeScript(activeTabId, { code: "window.onscroll = function () { console.log($(window).scrollTop()) };" }, function (top) {
            //console.log("top: ")
            //console.log(top)
            //console.log()

            if (top != null && top !== prevActiveTabScroll) {
                updateScroll(activeTabId, top)
            }
            prevActiveTabScroll = top;
        });

        chrome.tabs.executeScript(activeTabId, { code: "console.log(document.documentElement.scrollTop);"})

        var doc = document.documentElement;
        var top = $(window).scrollTop();
        console.log(top);
        if (top != null && top !== prevActiveTabScroll) {
            updateScroll(activeTabId, top)
        }
        prevActiveTabScroll = top;*/


        updateScrollFromId(activeTabId);
        setTimeout(activeTabPoll, 100);
    }
    activeTabPoll();

    var addTab = function(tab) {
        tabs[tab.id] = tab;

        if (tab.active) {
            activeTabId = tab.id;
        }

        updateScrollFromId(tab.id);

        updateTab(tab.id);
    }

    //callback to get all existing tabs
    var initTabsCallback = function(tabArray) {
        tabArray.forEach(function(tab) {
            addTab(tab);
        });
    }

    //callback for when a new tab is created
    var newTabCallback = function (newTab) {
        console.log("newTabCallback called");
        addTab(newTab);
    }

    //callback for when a tab is updated
    var tabUpdatedCallback = function (tabId, changeInfo, tab) {
        console.log("tab updated with change info: ");
        console.log(changeInfo);
        updateTab(tabId);
    }

    //callback to get all existing tabs
    var activeTabChangedCallback = function (tabId) {
        activeTabId = tabId;
        console.log("active tab set to: " + tabId);
        updateTab(tabId);
        setTimeout(function() { sendTabScreenshot(tabId); }, 2000);

    }

    chrome.tabs.query({}, initTabsCallback);
    chrome.tabs.onCreated.addListener(newTabCallback);
    chrome.tabs.onUpdated.addListener(tabUpdatedCallback);
    chrome.tabs.onActiveChanged.addListener(activeTabChangedCallback);

    this.setScrollPosition = function (tabId, positionY) {
        console.log("position y asked to be set to: "+positionY)
        var script = "window.scrollTo(0, " + positionY + ");document.documentElement.scrollTop = "+positionY+";";
        chrome.tabs.executeScript(tabId, { code: script });
    }

    var thisSetScrollPosition = this.setScrollPosition;

    this.addNewTab = function (url)
    {   
        var tabHandler = function (tab) {
            //console.log("in tab handler");
            //console.log(tab);
            thisSetScrollPosition(tab.id, 150);
        }

        //console.log("Creating new tab with url: " + url)
        chrome.tabs.create({url: url}, tabHandler);
    }

    this.setTabUrl = function (tabId, url) {
        chrome.tabs.update(tabId, { url: url });
    }

    this.sendAllTabs = function () {
        Object.keys(tabs).forEach(function (tabId) {
            updateTab(parseInt(tabId));
        });
    }
}