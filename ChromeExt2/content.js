
(function() {

    var isEnabled = false;

    chrome.runtime.onMessage.addListener( function(message) {
        isEnabled = message.tableExtract;
        Array.from(document.getElementsByTagName("table")).forEach(table => {
            table.draggable = isEnabled
        });
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

    Array.from(document.getElementsByTagName("table")).forEach(table => {
        table.addEventListener("dragstart", (e) => {
            e.dataTransfer.setData("text/plain", JSON.stringify(tableToJson(e.currentTarget)))
            e.dataTransfer.setData("text/tabledrop", JSON.stringify(tableToJson(e.currentTarget)))
            if (isEnabled) {
            
            }
        });
    });

})();