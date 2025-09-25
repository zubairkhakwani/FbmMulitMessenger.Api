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
};
