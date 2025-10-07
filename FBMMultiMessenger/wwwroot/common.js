window.myInterop = {
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },
    getItem: function (key) {
        return localStorage.getItem(key);
    },
    removeItem: function (key) {
        localStorage.removeItem(key);
    },
    playNotificationSound: function (volume) {
        const audio = new Audio('/sounds/notification.mp3');
        audio.volume = volume;
        audio.play();
    },
    handleMessageFailed: function () {
        console.log("Message failed to send");
    },
    createObjectURL: function (file) {
        try {
            console.log(file.object);
            if (file?.object) {
                return URL.createObjectURL(file.object);
            } else if (file instanceof Blob) {
                return URL.createObjectURL(file);
            } else {
                console.warn("Invalid file type passed to createObjectURL:", file);
                return null;
            }
        } catch (e) {
            console.error("Error creating object URL:", e);
            return null;
        }
    },
   
    showSweetAlert: function (title, message, showFooter = false, footerText = "Help", footerLink = "#", icon = "error") {
        const config = {
            icon: icon,
            title: title || "Error",
            text: message || "Something went wrong."
        };

        if (showFooter) {
            config.footer = `<a href="${footerLink}">${footerText}</a>`;
        }

        Swal.fire(config);
    }
};
