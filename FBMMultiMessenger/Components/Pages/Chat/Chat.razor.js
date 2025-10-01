const accountChats = document.querySelector(".chat-list") // accounts chats that are being loaded in the side bar.
const messageContainer = document.querySelector(".messages-container"); // Actual chat messages.
const messageList = messageContainer.querySelector("#messagesList");
const mainChat = document.querySelector("#mainChat");
const header = document.querySelector(".chat-header");

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
                AddEvenListenerToAccountChats();
            }
        }
    }
});
observer.observe(accountChats, { childList: true, subtree: true, });
observer.observe(messageContainer, { childList: true, subtree: true, });


function AddEvenListenerToAccountChats() {
    document.querySelectorAll('.chat-item').forEach(item => {
        item.addEventListener('click', function () {
            let activeItem = document.querySelector('.chat-item.active');
            //Make previously selected chat to not selected.
            if (activeItem) {
                activeItem.classList.remove('active');
            }

            //Make the chat selected.
            item.classList.add('active');

            //This will show the top bar and nav bar only when the chat gets selected.
            mainChat.classList.remove("no-chat-selected");

            let chatName = item.querySelector(".chat-name");
            let listingPrice = item.querySelector(".listing-price");
            let listingLocation = item.querySelector(".listing-location");

            let headerChatName = header.querySelector("h3");
            let headerParagraph = header.querySelector("p");
            headerChatName.textContent = chatName.textContent;
            headerParagraph.textContent = `${listingLocation.textContent} - ${listingPrice.textContent}`


            messageContainer.style.height = "calc(100vh - 135px)";
        });
    });
}


