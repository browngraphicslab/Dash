

posX = 0;
posY = 0;

selectionId = 0;
mode = "document"
documentType = "website";
selectionType = "website";
selectionSource = "";
imgHovered = false;
isActive = false;
iFrameMain= null;
iFrameInlineSave = null;



chrome.runtime.onMessage.addListener(function(request, sender, sendResponse) {

    if (request.msg == "init") {
      
        nusys_init(request.args);
    }
    if (request.msg == "activate") {
        nusys_activate();
    }

    if (request.msg == "deactivate") {
        nusys_deactivate();
    }

    if (request.msg == "button") {
        changeButtonText(request.args);
    }
});

function nusys_init(htmlSources) {
    if(window.hasOwnProperty("content_script_initialized"))
        return;
    content_script_initialized = true;
    console.log("initializing menu.")
    console.log(htmlSources)

    iFrameMain = createIFrame(htmlSources.menuMain);
    iFrameMainDoc = createIFrame(htmlSources.menuMainDoc);
    iFrameTags = createIFrame(htmlSources.menuTags);
    iFrameInlineSave = createIFrame(htmlSources.menuInlineSave)
    iFrameInlineField = createIFrame(htmlSources.menuInlineField)
  
    $(iFrameMain).css({ position: "fixed", width: "140px", height: "60px", top: "70px", right: "50px", margin: 0, padding: 0, border:0, "z-index": 10000000000, "display": "block" });
    $(iFrameMainDoc).css({ position: "fixed", width: "300px", height: "350px", top: "70px", right: "50px", margin: 0, padding: 0, border:0, "z-index": 10000000000, "display": "none" });
    $(iFrameTags).css({ position: "fixed", width: "300px", height: "350px", top: "70px", right: "50px", margin: 0, padding: 0, border:0, "z-index": 10000000000, "display": "none" });
    $(iFrameInlineSave).css({ position: "fixed",  width: "140px", height: "60px", top: "1px", right: "900px", margin: 0, padding: 0, border:0, "z-index": 10000000000, "display": "none" });
    $(iFrameInlineField).css({ position: "fixed", width: "300px", height: "300px", top: "1px", right: "1200px", margin: 0, padding: 0, border:0, "z-index": 10000000000, "display": "none" });

    $('html > head').append($('<style id="nusys_highlighter">::selection {  background: rgba(199,222,222,0.8); /* WebKit/Blink Browsers */}</style>'));  
    var a = htmlSources.css;
    $(iFrameMain).contents().find("head").append($("<style>" + a + "</style>"));
    $(iFrameMainDoc).contents().find("head").append($("<style>" + a + "</style>"));
    $(iFrameTags).contents().find("head").append($("<style>" + a + "</style>"));
    $(iFrameInlineSave).contents().find("head").append($("<style>" + a + "</style>"));
    $(iFrameInlineField).contents().find("head").append($("<style>" + a + "</style>"));

    console.log(iFrameTags)


    resizeToContent(iFrameMain);
    resizeToContent(iFrameMainDoc);
    resizeToContent(iFrameTags);
    //resizeToContent(iFrameInlineSave);
    resizeToContent(iFrameInlineField);
    

    clearSelection();

    

    $(window).scroll(function() {
        if (!isActive)
            return;

        clearSelection();
    });



    $(iFrameMain.contentWindow.document).keyup(function(e) {
        if (!isActive)
            return;
        if (e.keyCode == 27) { 
            clearSelection();
            document.body.focus()
        }
    });

    $(iFrameMain).contents().find("#btnSend").on("dragstart", function(evt) {
        var data = {}
        data.msg = "selection";
        data.type = selectionType;
        data.selectionId = null;
        data.url = window.location.href;
        data.data = selectionData;
        evt.originalEvent.dataTransfer.setData("text/plain", JSON.stringify(data));    
    });

    $(iFrameMain).contents().find("#btnNewDoc").click(function(){
        $(iFrameMainDoc).contents().find("#fields li").not(".nofields").remove();
        $(iFrameMain).css({display: "none"});
        $(iFrameMainDoc).css({display: "block"});
        mode = "fields";
        chrome.extension.sendMessage({msg: "getSelectionId"}, function(response) {
            
            selectionId = response.id;

            var data = {}
            data.msg = "newdoc";
            data.selectionId = response.id;
            data.url = window.location.href;
            chrome.extension.sendMessage(data);
        });       


    });

    $(iFrameMainDoc).contents().find(".btnClose").click(function(){
        $(iFrameMain).css({display: "block"});
        $(iFrameMainDoc).css({display: "none"});
        mode = "document";
    });

    $(iFrameMainDoc).contents().find("#btnAddTags").click(function(){
        mode = "document"
        $(iFrameTags).css({display: "block"});
        $(iFrameMainDoc).css({display: "none"});
        $(iFrameTags).contents().find("#tagfield").focus();
    });


    $([iFrameMain, iFrameInlineSave]).contents().find("#btnSend").click(function(e){

        if (!isActive)
            return;

        chrome.extension.sendMessage({msg: "getSelectionId"}, function(response) {
            
            selectionId = response.id;

            var data = {}
            data.msg = "selection";
            data.type = selectionType;
            data.selectionId = response.id;
            data.url = window.location.href;
            data.data = selectionData;
            data.ratio = document.documentElement.clientWidth / document.documentElement.clientHeight;
            chrome.extension.sendMessage(data)

            var iframepos = $(iFrameInlineSave).position();
            posX = iframepos.left + e.clientX;
            posY = iframepos.top + e.clientY;
            showTags();
        });       
        
    });

    var tagfield = $(iFrameTags).contents().find("#tagfield")
     tagfield.keyup(function(event){
        if(event.keyCode == 13) {
            tag = tagfield.val();
            $(iFrameTags).contents().find("#tags").prepend("<li>" + tag + "</li>");
            $(iFrameTags).contents().find(".notags").remove();
            var data = {}
            data.selcetionId = selectionId;
            data.msg = "tag";
            data.data = tag;
            chrome.runtime.sendMessage(data)
            tagfield.val("");
        }
    });

    var field = $(iFrameInlineField).contents().find("#tagfield")
    field.keyup(function(event){
        if(event.keyCode == 13) {
            tag = field.val();
            $(iFrameMainDoc).contents().find("#fields").prepend("<li>" + tag + "</li>");
            $(iFrameMainDoc).contents().find(".nofields").remove();
            var data = {}
            data.selcetionId = selectionId;
            data.msg = "field";
            data.data = {fieldname: tag, value: getSelectionText()};
            chrome.runtime.sendMessage(data)
            field.val("");
            $(iFrameInlineField).css({display: "none"});
        }
    });

    $(iFrameTags).contents().find("#recentTags li").click(function(event){
        var tag = $(this).text()
        $(iFrameTags).contents().find("#tags").prepend("<li>" + tag + "</li>");
        $(iFrameTags).contents().find(".notags").remove();
        var data = {}
        data.selcetionId = selectionId;
        data.msg = "tag";
        data.data = tag;
        chrome.runtime.sendMessage(data)
    });

    
    $(iFrameInlineField).contents().find("#recentTags li").click(function(event){
        var tag = $(this).text()
        $(iFrameMainDoc).contents().find("#fields").prepend("<li>" + tag + "</li>");
        $(iFrameMainDoc).contents().find(".nofields").remove();
        var data = {}
        data.selcetionId = selectionId;
        data.msg = "field";
        data.data = {fieldname: tag, value: getSelectionText()};
        chrome.runtime.sendMessage(data)
        $(iFrameInlineField).css({display: "none"});
    });

 
    if (window.location.href.includes(".pdf")){
        init_pdf_selection();
    }
    else if (window.location.href.includes("www.youtube.com/watch")){
        init_youtube_selection();
    }
    else {
        init_website_selection();
    }
}

function init_pdf_selection() {


    documentType = "pdf";
    selectionType = "pdf";
    selectionData = $("embed").attr("src");
    changeButtonText("Save PDF")
    showButtonOnPdf();

    function callback(c) {
        if (c.data.type == "getSelectedTextReply") {            
            if (c.data.selectedText != "" ) {
                selectionType = "text";
                selectedPdfText = c.data.selectedText;
                selectionData = c.data.selectedText;
                changeButtonText("Save Text");
                showButtonNextToText(posX, posY);
            } else {
                changeButtonText("Save PDF");
                clearSelection();
            }
        } else {
    
            clearSelection();
        }
    }

    window.addEventListener("message", callback);

    $(document.body).mouseup(function(e) {
        if (!isActive)
            return;
        posX = e.clientX;
        posY = e.clientY;
        selectionType = "pdf";
        selectionData = $("embed").attr("src");
        document.getElementsByTagName("embed")[0].postMessage({ type: "getSelectedText"});        
    });
}

function init_youtube_selection() {
    documentType = "youtube";
    selectionType = "video";
    selectionData = window.location.href;
    changeButtonText("Save Video")
    showButtonOnYouTube();

    $(document.body).mouseup(function(e) {
        if (!isActive)
            return;
        clearSelection();
    });

}

function init_website_selection() {
    documentType = "website";
    selectionData = window.location.href;
    showButton();
    $(document.body).mouseup(function(e) {
        if (!isActive)
            return;
        window.setTimeout(function(){
            if (window.getSelection().isCollapsed || getSelectionText() == "") {
                $(iFrameMain).css({"display": "block"})
                $(iFrameInlineSave).css({"display": "none"})
                clearSelection();
            }
            else {
                $(iFrameMain).css({"display": "none"})
                if (mode == "document") {
                    selectionType = "text";
                    selectionData = getSelectionText();
                    changeButtonText("Save Text");
                    showButtonNextToText(e.clientX, e.clientY);
                } else {
                    $(iFrameInlineField).css({"display": "block", "top": e.clientY + 20, "left": e.clientX - $(iFrameInlineSave).width()/2 })
                    $(iFrameInlineField).css({"display": "block"})
                    $(iFrameInlineField).contents().find("#tagfield").focus();
                }  
            }
        },30);
    });

    $("img").mouseover(function(e) {

        if (!isActive)
            return;

        if ($(this).width()< 50 && $(this).height()<50)
            return;

        selectionType = "img";
        selectionData = (new URL($(this)[0].src)).toString();
        imgHovered = true;
        window.getSelection().empty();
        changeButtonText("Save Image");
        var offset = $(this).offset();
        $(iFrameInlineSave).css({"display": "block", "top": offset.top +  $(this).height()/2 -$(iFrameInlineSave).height()/2 - window.pageYOffset, "left": offset.left + $(this).width()/2 - $(iFrameInlineSave).width()/2 +12 })
    });
    $("img").mouseleave(function(e){
        if (!isActive)
            return;
        imgHovered = false;
        if ($("iFrame:hover").length == 0) {
           setTimeout(clearSelection, 100);
        } else {
            console.log("switched from img to button")
        }
    });
}

function nusys_deactivate() {
    $("nusys_highlighter").remove();
    $(iFrame).css({"display": "none"});
    isActive = false;
}

function nusys_activate() {
    console.log("activating..")
    $('html > head').append($('<style id="nusys_highlighter">::selection {  background: rgba(199,222,222,0.8); /* WebKit/Blink Browsers */}</style>')); 
    isActive = true;
    clearSelection();
}

function getSelectionText() {
    var text = "";
    if (window.getSelection) {
        text = window.getSelection().toString();
    } else if (document.selection && document.selection.type != "Control") {
        text = document.selection.createRange().text;
    }
    return text;
}

function getSelectionDimensions() {
    var sel = document.selection, range;
    var width = 0, height = 0, top = 0, left = 0;
    if (sel) {
        if (sel.type != "Control") {
            range = sel.createRange();
            width = range.boundingWidth;
            height = range.boundingHeight;
            top = range.top;
            left = range.left;
        }
    } else if (window.getSelection) {
        sel = window.getSelection();
        if (sel.rangeCount) {
            range = sel.getRangeAt(0).cloneRange();
            if (range.getBoundingClientRect) {
                var rect = range.getBoundingClientRect();
                width = rect.right - rect.left;
                height = rect.bottom - rect.top;
                top = rect.top;
                left = rect.left;
            }
        }
    }
    return { top: top, left: left, width: width , height: height };
}

function clearSelection() {
    if (documentType == "pdf") {
        selectionType = "pdf";
    } else if (documentType == "youtube") {
        selectionType = "video";
    }  else {
        selectionType = "website";
    }

    selectedPdfText = "";
    imgHovered = false;
    if (documentType == "website") {
        showButton();
    } else if (documentType == "pdf") {
        showButtonOnPdf();
    } else if (documentType == "youtube") {
        showButtonOnYouTube();
    } else {
        throw new Error("error");
    }

    

    $(iFrameTags).contents().find("#tags li").not(".notags").remove();
    $(iFrameTags).css({display: "none"});
    $(iFrameInlineField).css({display: "none"});
}

function showButtonNextToText(mouseX, mouseY) {
    
    $(iFrameInlineSave).contents().find(".btnSendText").text("Save Text");  
    $(iFrameInlineSave).css({"display": "block", "top": mouseY + 20, "left": mouseX - $(iFrameInlineSave).width()/2 })

}

function showButtonOnPdf() {
    $(iFrameInlineSave).css({"display": "none"}); 
    $(iFrameMain).contents().find("#btnSend").text("Save PDF");  

}

function showButtonOnYouTube() {
    $(iFrameInlineSave).css({"display": "none"}); 
    $(iFrameMain).contents().find("#btnSend").text("Save Video");  

}


function showButton() {
    $(iFrameMain).contents().find("#btnSend").text("Save Website");  
}

function showTags() {
    
    if (hasSelection()) {
        moveIFrame( posX - $(iFrameTags).width()/2, posY - $(iFrameTags).height()/2), iFrameTags;
    }
    else if (documentType == "website") {
         moveIFrame( $(window).width() - $(iFrameTags).width() -50, 70, iFrameTags);
    }
    else if (documentType == "pdf") {
         moveIFrame( $(window).width() - $(iFrameTags).width() -50, 70, iFrameTags);
    }
    else if (documentType == "youtube") {
        
         moveIFrame( $(window).width() - $(iFrameTags).width() -50, 70, iFrameTags);
    }
    else {
        var t= $(iFrameTags).offset().top -  $(iFrameTags).height()/2;
        $(iFrameTags).offset({top: t})  
        console.log("wooo")
    }

    changeButtonText("")

    $(iFrameTags).contents().find("#tagfield").focus();
    $(iFrameInlineSave).css({display: "none"});
}

function hasSelection() {
    return selectedPdfText != "" || getSelectionText() != "" || $(iFrameInlineSave).contents().find(".btnSendText").text() == "Save Image";
}

function moveIFrame(x,y, iframe) {
    var x = Math.max(10, Math.min(x, $(window).width() - $(iFrameTags).width() - 10));
    var y = Math.max(10, Math.min(y, $(window).height() - $(iFrameTags).height() - 10)); 
    $(iFrameTags).css({"display": "block", "top": y, "left": x });
}

function isPdfFile(response, url) {
  var header = response.getResponseHeader('content-type');
  if (header) {
    var headerValue = header.toLowerCase().split(';', 1)[0].trim();
    return (headerValue === 'application/pdf' ||
            headerValue === 'application/octet-stream' &&
            url.toLowerCase().indexOf('.pdf') > 0);
  }
}

function changeButtonText(text) {
    $(iFrameMain).contents().find(".btnSendText").text(text); 
    $(iFrameInlineSave).contents().find(".btnSendText").text(text); 
}

function createIFrame(htmlSource) {
    var f = $("<iframe frameborder=0 />")[0];
    $("body").append(f);
    var doc = f.contentWindow.document;
    doc.open();
    doc.write(htmlSource);
    doc.close();

    return f;
}

function resizeToContent(iFrame){
    $(iFrame).width(iFrame.contentWindow.document.body.scrollWidth);
    $(iFrame).height(iFrame.contentWindow.document.body.scrollHeight);
}