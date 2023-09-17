let dragButtons = document.getElementsByClassName("drag-button");
let listItems = document.getElementsByClassName("draggable");
let gameList = document.getElementById("game-list");
let draggedItem = null;
let draggedItemCopy = null;
let draggedItemWidth = 0;
let xOffset = 0;
let yOffset = 0;
let dropPosition = 0;

// Setup dragging
for (let button of dragButtons) {
    button.addEventListener("mousedown", (event) => {
        draggedItem = GetParentLi(event.target);
        draggedItem.classList.remove("draggable");
        
        // Calculate the offset to have the mouse over the drag button while dragging
        let targetRect = event.target.getBoundingClientRect();
        let draggedRect = draggedItem.getBoundingClientRect();
        let draggedStyles = window.getComputedStyle(draggedItem);
        draggedItemWidth = draggedRect.width;
        xOffset = draggedRect.x - targetRect.x - Number(draggedStyles.marginLeft.replace("px", "")) - (targetRect.width / 2);
        yOffset = draggedRect.y - targetRect.y - Number(draggedStyles.marginTop.replace("px", "")) - (targetRect.height / 2);
        
        document.addEventListener("mousemove", FollowMouse);
        
        draggedItem.style.pointerEvents = "none";   // Stops the mouseover from picking up the dragged item
        
        // Create shadow copy to give preview of drop
        draggedItemCopy = draggedItem.cloneNode(true);
        draggedItemCopy.id = null;
        draggedItemCopy.style.opacity = "0.5";
        draggedItemCopy.classList.remove("draggable");
        gameList.appendChild(draggedItemCopy);
        draggedItem.after(draggedItemCopy);

        // Set initial styles for the dragged item
        draggedItem.style.position = "absolute";
        draggedItem.style.zIndex = "50";
        draggedItem.style.minWidth = `${draggedItemWidth}px`;
        draggedItem.style.left = `${event.pageX + xOffset}px`;
        draggedItem.style.top = `${event.pageY + yOffset}px`;
    });
}

document.addEventListener("mouseup", (event) => {
    UnFollowMouse();
    draggedItem = null
    
    if (draggedItemCopy !== null) {
        draggedItemCopy.remove();
        draggedItemCopy = null;
    }
});

for (let listItem of listItems) {
    listItem.addEventListener("mouseover", (event) => {
        let parentLi = GetParentLi(event.target);
        let rank = Number(parentLi.dataset.rank);
        
        // If not dragging, we want the drop position to be the rank
        // Avoids weirdness at the beginning of dragging
        if (event.buttons === 0) {
            dropPosition = rank;
            return;
        }

        // Determine how the rank would change 
        // based on whether it was dragged up or down from its original position
        let draggedRank = Number(draggedItem.dataset.rank);
        let upRank = draggedRank;
        let downRank = draggedRank;
        if (rank < draggedRank) {
            upRank = rank + 1;
            downRank = rank;
        }
        if (rank > draggedRank) {
            upRank = rank;
            downRank = rank - 1;
        }
        
        // Calculate dropping position based on whether the top or bottom half of an item is hovered
        let bounding = parentLi.getBoundingClientRect()
        let offset = bounding.y + (bounding.height / 2);
        if ( event.pageY - offset > 0 ) {
            dropPosition = upRank;
            if (draggedItemCopy !== null) {
                parentLi.after(draggedItemCopy);
            }
        } else {
            dropPosition = Math.max(downRank, 1);   // Clamps to 1 or above
            if (draggedItemCopy !== null) {
                parentLi.before(draggedItemCopy);
            }
        }
    });
}

// Moves up the tree to find the parent list item
let GetParentLi = (element) => {
    while (element.tagName.toUpperCase() !== "LI" && element.tagName.toUpperCase() !== "BODY") {
        element = element.parentElement;
    }
    
    if (element.tagName.toUpperCase() === "BODY") {
        return null;
    }
    
    return element;
}

let FollowMouse = (event) => {
        if (draggedItem) {
            draggedItem.style.left = `${event.pageX + xOffset}px`;
            draggedItem.style.top = `${event.pageY + yOffset}px`;
            
            // If dragged outside of the list, reset position
            if (event.target.closest("ul") === null) {
                dropPosition = 1;
                if (draggedItemCopy !== null) {
                    draggedItem.after(draggedItemCopy);
                }
            }
        }
}

let UnFollowMouse = (event) => {
    document.removeEventListener("mousemove", FollowMouse)
    
    if (draggedItem) {
        draggedItem.style.position = "static";
        draggedItem.style.zIndex = null;
        draggedItem.style.minWidth = null;
        draggedItem.style.left = "0";
        draggedItem.style.top = "0";
        draggedItem.style.pointerEvents = null;
    }
}

export let GetDropPosition = () => Number(dropPosition);