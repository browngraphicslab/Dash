window.onkeyup = function(e) {
   var key = e.keyCode ? e.keyCode : e.which;
      if (event.keyCode == 78) {
            chrome.extension.sendMessage({msg: "activate"}, function(response) {
            
        });    
    }
}