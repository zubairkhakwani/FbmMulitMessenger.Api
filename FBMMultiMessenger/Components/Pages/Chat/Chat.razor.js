(function () {
    let accountChats = document.querySelector(".chat-list") // accounts chats that are being loaded in the side bar.
    let messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
    let messageList = messageContainer.querySelector("#messagesList");
    let mainChat = document.querySelector("#mainChat");
    let header = document.querySelector(".chat-header");

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
                else if (mutation.target === accountChats) {
                    console.log("Adding Event listeners");
                    AddEvenListenerToAccountChats();
                }
            }
        }
    });
    observer.observe(accountChats, { childList: true, subtree: true, });
    observer.observe(messageContainer, { childList: true, subtree: true, });


    function AddEvenListenerToAccountChats() {
      
        document.querySelectorAll('.chat-item').forEach(item => {
            console.log(item);
            item.addEventListener('click', function () {
                let activeItem = document.querySelector('.chat-item.active');
                //Make previously selected chat to not selected.
                if (activeItem) {
                    activeItem.classList.remove('active');
                }
                //Make the chat selected.
                item.classList.add('active');


                messageContainer.style.height = "calc(100vh - 135px)";
            });
        });
    }



})();
