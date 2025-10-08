(function () {
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let inputWrapper = document.querySelector(".input-wrapper");
    let dotnetHelper;

    window.registerEnterHandler = (ref) => {
        dotnetHelper = ref;
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
                    messageInput.onkeydown = async e => {
                        if (e.key === "Enter" && !e.shiftKey) {
                            if (!messageInput) { console.log("Cannot find message input"); return; }

                            let message = messageInput.value;
                            messageInput.value = "";
                            e.preventDefault();
                            await dotnetHelper.invokeMethodAsync('HandleEnterKey', message);
                            
                        }
                    };

                }
            }
        }
    });
    observer.observe(messageContainer, { childList: true, subtree: true, });
    observer.observe(inputWrapper, { childList: true, subtree: true, })





})();
