window.displayNotification = (text, notificationLevel) => {
    const notificationDiv = document.getElementById("notification");
    
    if (notificationDiv.childElementCount >= 5)
        return;
    
    if (!notificationLevel)
        notificationLevel = NotificationLevel.Info;
    
    const notificationElement = document.createElement("div");
    notificationElement.className = "w-80 py-2 px-4 m-2 drop-shadow-md border rounded-md font-semibold text-ellipsis overflow-hidden hover:drop-shadow-lg";
    notificationElement.onclick = () => notificationElement.remove();
    notificationElement.innerText = text;
    
    switch (notificationLevel) {
        case NotificationLevel.Info:
            notificationElement.className += " bg-blue-100 border-blue-500 text-blue-700 hover:bg-blue-50";
            break;
        case NotificationLevel.Success:
            notificationElement.className += " bg-green-100 border-green-500 text-green-700 hover:bg-green-50";
            break;
        case NotificationLevel.Warning:
            notificationElement.className += " bg-yellow-100 border-yellow-500 text-yellow-700 hover:bg-yellow-50";
            break;
        case NotificationLevel.Error:
            notificationElement.className += " bg-red-100 border-red-500 text-red-700 hover:bg-red-50";
            break;
    }
    
    notificationDiv.appendChild(notificationElement);
    
    setTimeout(() => notificationElement.remove(), 5 * 1000);
}

const NotificationLevel = {
    Info: 0,
    Success: 1,
    Warning: 2,
    Error: 3
}