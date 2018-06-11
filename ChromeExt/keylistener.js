/*
document.body.style.backgroundColor = "yellow";
var button = document.createElement("button");
button.style.width = "50px";
button.style.height = "50px";
button.style.backgroundColor = "red";
button.style.postion = "fixed";
button.style.margin = "100px";
document.body.appendChild(button);
window.alert("hey!");
*/

window.onkeyup = function (e) {
   var key = e.keyCode ? e.keyCode : e.which;
      if (event.keyCode == 78) {
            chrome.extension.sendMessage({msg: "activate"}, function(response) {
            
        });    
    }
}