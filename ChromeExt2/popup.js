document.getElementById("btnSticky").addEventListener("click", function() {
    chrome.windows.getCurrent(function(e) {
        chrome.runtime.sendMessage("toggleActive");
    })
});

chrome.runtime.onMessage.addListener( function(message) {
    var button = document.getElementById("btnSticky");
    var status = document.getElementById("status");
    if (message == "status:connected") {
        status.classList.add("active");
        button.innerText = "Disconnect";
        status.innerText = "Connected.";
    } else if (message == "status:disconnected") {
        status.classList.remove("active");
        status.innerText = "Disconnected.";
        button.innerText = "Connect";
    } else if (message == "status:error") {
        status.classList.remove("active");
        status.innerText = "Error: No Service running.";
        button.innerText = "Connect";
    }
});

document.getElementById("tblExtract").addEventListener("click", function () {

    //chrome.tabs.executeScript(chrome.tabs.getSelected(null, function (tab) {
    //    (tab);
    //}), {
    //    "code":
    //        'return document.getElementsByTagName("table");'
    //    }, function (result) {
    //    console.log(result);
    //    });
    chrome.tabs.getSelected(null, function(tab) {
        var url = tab.url;
        var getHTML = function (url, callback) {

            // Feature detection
            if (!window.XMLHttpRequest) return;

            // Create new request
            var xhr = new XMLHttpRequest();

            // Setup callback
            xhr.onload = function () {
                if (callback && typeof (callback) === 'function') {
                    callback(this.responseXML);
                }
            }

            // Get the HTML
            xhr.open('GET', url);
            xhr.responseType = 'document';
            xhr.send();

        };
        getHTML(url,
            function(response) {
                var tables = response.getElementsByTagName('table');
                console.log(tables);
                var data = [];
                for (var table of tables) {
                    data.push(tableToJson(table));
                }
                chrome.windows.getCurrent(function(e) {
                    chrome.runtime.sendMessage({ "type": "extractTable", "data": data });
                });
            });
    });;
});

function tableToJson(table) {
    if (table !== null) {
        console.log(table);
        var data = [];
        var headers = [];
        for (var i = 0; i < table.rows[0].cells.length; i++) {
            headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(' ', '');
        }

        for (var i = 1; i < table.rows.length; i++) {
            var tableRow = table.rows[i];
            var rowData = {};

            for (var j = 0; j < tableRow.cells.length; j++) {
                rowData[headers[j]] = tableRow.cells[j].textContent;
            }

            data.push(rowData);
        }
        return data;
    }
}

chrome.runtime.sendMessage("requestStatus");