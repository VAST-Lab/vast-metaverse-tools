mergeInto(LibraryManager.library, {
    OpenURLInNewTab: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        window.open(url, '_blank');
    }
});