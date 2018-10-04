document.getElementById("btnSticky").addEventListener("click", function() {
    chrome.windows.getCurrent(function(e) {
        chrome.runtime.sendMessage("toggleActive");
    })
}); 

chrome.runtime.onMessage.addListener( function(message) {
    var buttonConnect = document.getElementById("btnSticky");
    var buttonTableExtract = document.getElementById("btnTblExtract");
    if (message.connected) {
        buttonConnect.classList.add("active");
    } else if (message.status == "disconnected") {
        buttonConnect.classList.remove("active");
    }

    if (message.tableExtract) {
        buttonTableExtract.classList.add("active")
    } else {
        buttonTableExtract.classList.remove("active")
    }
});

document.getElementById("btnTblExtract").addEventListener("click", function () {   
    chrome.windows.getCurrent(function(e) {
        chrome.runtime.sendMessage({action: "toggleTableExtract" });
    })
});

function tableToJson(table) {
    if (table !== null) {
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

chrome.runtime.sendMessage({action: "requestStatus"});