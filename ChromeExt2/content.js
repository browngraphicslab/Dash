
(function() {

    var isEnabled = false;

    chrome.runtime.onMessage.addListener( function(message) {
        isEnabled = message.tableExtract;
        Array.from(document.getElementsByTagName("table")).forEach(table => {
            table.draggable = isEnabled
        });
    });

    let textfield = document.createElement("textarea");
    textfield.style.position = "absolute";
    textfield.style.left = "-10000px";
    document.body.appendChild(textfield);

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

    document.addEventListener('copy', function(e){
        e.clipboardData.setData('text/plain', textfield.value);
        e.clipboardData.setData('text/tabledrop', textfield.value);
        e.preventDefault(); // We want to write our data to the clipboard, not data from any user selection
    });

    Array.from(document.getElementsByTagName("table")).forEach(table => {
        table.addEventListener("pointerdown", (e) => {
            if (isEnabled) {
                textfield.value = JSON.stringify(tableToJson(e.currentTarget));
                textfield.select();
                document.execCommand("copy");
            }
        });
    });

})();