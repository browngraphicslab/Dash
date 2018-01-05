
function tabManager(sendRequestFunction) {
    var tabs = {}
    var activeTabId = 0;

    var updateTab = function(tabId) {
        var update = function (tab) {
            if (tab != null) {
                var requestBody = {
                    "$type": "Dash.UpdateTabBrowserRequest, Dash",
                    "tabId": tab.id,
                    "current": tab.active,
                    "url": tab.url,
                    "title": tab.title,
                    "index": tab.index,
                }
                sendRequestFunction(requestBody)
            }
            else
            {
                console.log("error: tab was null.  ID: "+tabId)
            }
        }
        chrome.tabs.get(tabId, update)
    }

    var addTab = function(tab) {
        tabs[tab.id] = tab;

        if (tab.active) {
            activeTabId = tab.id
        }

        updateTab(tab.id)
    }

    //callback to get all existing tabs
    var initTabsCallback = function(tabArray) {
        tabArray.forEach(function(tab) {
            addTab(tab);
        })
    }

    //callback for when a new tab is created
    var newTabCallback = function (newTab) {
        console.log("newTabCallback called")
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
    }

    chrome.tabs.query({}, initTabsCallback);
    chrome.tabs.onCreated.addListener(newTabCallback);
    chrome.tabs.onUpdated.addListener(tabUpdatedCallback);
    chrome.tabs.onActiveChanged.addListener(activeTabChangedCallback);

    this.setScrollPosition = function(tabId, positionY)
    {
        var script = "window.scrollTo(0, " + positionY + ");"
        chrome.tabs.executeScript(tabId, { code: script });
    }

    var test = this.setScrollPosition

    this.addNewTab = function (url)
    {   
        var tabHandler = function (tab) {
            //console.log("in tab handler");
            //console.log(tab);
            test(tab.id, 150);
        }

        //console.log("Creating new tab with url: " + url)
        chrome.tabs.create({url: url}, tabHandler);
    }

    this.setTabUrl = function (tabId, url) {
        chrome.tabs.update(tabId, { url: myNewUrl });
    }

    this.sendAllTabs = function () {
        Object.keys(tabs).forEach(function (tabId) {
            updateTab(parseInt(tabId))
        });
    }
}