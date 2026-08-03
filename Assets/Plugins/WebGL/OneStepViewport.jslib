mergeInto(LibraryManager.library, {
  OneStepRegisterViewportListener: function (objectNamePointer) {
    const objectName = UTF8ToString(objectNamePointer);
    const notify = function () {
      if (typeof SendMessage === "function") {
        SendMessage(objectName, "OnBrowserViewportChanged", "resize");
      }
    };

    window.addEventListener("resize", notify, { passive: true });
    window.addEventListener("orientationchange", notify, { passive: true });
    document.addEventListener("fullscreenchange", notify);
    window.visualViewport?.addEventListener("resize", notify, { passive: true });
  }
});
