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

chrome.runtime.sendMessage("requestStatus");