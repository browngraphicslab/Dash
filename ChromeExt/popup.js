document.addEventListener('DOMContentLoaded', function () {
    
   // var openDashButton = document.getElementById('openDash');
   // openDashButton.addEventListener('click', function () {
        


        chrome.tabs.getSelected(null, function (tab) {
            //tab.url can now be used to get url
            document.getElementById("openDash").innerHTML =
                "The full URL of this page is:<br>" + tab.url;
            var url = tab.url;
            if (url.includes("docs.google.com")) {
                //google doc is open
                document.body.style.backgroundColor = "red";
                document.getElementById("addDoc").style.visibility = "visible";
            }
        });
   // }, false);
}, false)