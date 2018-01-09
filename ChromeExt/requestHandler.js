
function requestHandler(tabManager) {
    //to handle a new tab request
    var handleNewTab = function(request)
    {
        newUrl = request.url;
        tabManager.addNewTab(newUrl);
    }

    //to handle a set url request
    var handleSetUrl = function (request) {
        newUrl = request.url;
        tabManager.setTabUrl(request.tabId, newUrl)
    }


    //takes in a string
    this.handle = function(message) {
        //console.log("Message: ")
        //console.log(message)
        if (message.includes("{")) {
            var obj = JSON.parse(message);
            var type = obj.$type.split(".").slice(-1)[0].split(",")[0]

            console.log(type)

            switch(type) {
                case "NewTabBrowserRequest":
                    handleNewTab(obj)
                    break;

                case "SetUrlRequest":
                    handleSetUrl(obj)
                    break;
            }
        }
        else if (message.toLowerCase() === "both") {
            tabManager.sendAllTabs();
        }

    }
}