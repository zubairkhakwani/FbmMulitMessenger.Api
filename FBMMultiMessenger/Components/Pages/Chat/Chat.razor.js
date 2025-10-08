(function () {
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let inputWrapper = document.querySelector(".input-wrapper");

    window.registerEnterHandler = (ref) => {
        window.dotnetHelper = ref;
        console.log("Reference:", ref)
        console.log("Dotnet helper from register enter handler", window.dotnetHelper);
    };

    const observer = new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            if (mutation.type === "childList") {
                if (mutation.target === messageList) {

                    messageContainer.scrollTo({
                        top: messageContainer.scrollHeight,
                        behavior: "smooth"
                    });

                    let lastMessage = messageList.lastElementChild;
                    lastMessage?.classList?.add("new-message");
                    setTimeout(() => {
                        lastMessage?.classList?.remove('new-message');
                    }, 600)
                }
                else if (mutation.target === inputWrapper) {
                    const messageInput = document.querySelector(".message-input");
                    if (!messageInput) { console.log("Cannot find message input"); return; }
                    //console.log(messageInput);
                    messageInput.onkeydown = async e => {
                        if (e.key === "Enter" && !e.shiftKey) {

                            let message = messageInput.value;
                            messageInput.value = "";
                            e.preventDefault();
                            console.log(dotnetHelper);
                            await window.dotnetHelper?.invokeMethodAsync('HandleEnterKey', message);
                        }
                    };
                }
            }
        }
    });
    observer.observe(messageContainer, { childList: true, subtree: true, });
    observer.observe(inputWrapper, { childList: true, subtree: true, })
})();
