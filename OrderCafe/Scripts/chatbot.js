(function() {
    const toggle = document.getElementById("cafe-chat-toggle");
    const panel = document.getElementById("cafe-chat-panel");
    const closeButton = document.getElementById("cafe-chat-close");
    const messages = document.getElementById("cafe-chat-messages");
    const form = document.getElementById("cafe-chat-form");
    const input = document.getElementById("cafe-chat-input");

    function setOpen(isOpen) {
        panel.hidden = !isOpen;
        toggle.setAttribute("aria-expanded", String(isOpen));

        if (isOpen) {
            input.focus();
        } else {
            toggle.focus();
        }
    }

    function addMessage(sender, message) {
        const bubble = document.createElement("p");
        const senderName = sender === "user" ? "Bạn" : "Bot";

        bubble.classList.add("cafe-chat-bubble", `cafe-chat-bubble--${sender}`);
        bubble.textContent = message;
        bubble.setAttribute("aria-label", `${senderName}: ${message}`);

        messages.appendChild(bubble);
        messages.scrollTop = messages.scrollHeight;
    }   

    form.addEventListener("submit", async function(event) {
        event.preventDefault();

        const message = input.value.trim();
        const sendButton = form.querySelector('button[type="submit"]');

        if (!message || sendButton.disabled) return;

        const formData = new FormData(form);
        formData.set("message", message);

        addMessage("user", message);
        input.value = "";
        sendButton.disabled = true;
        sendButton.textContent = "Đang trả lời…";

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData
            });

            if (!response.ok) throw new Error("Chat request failed");

            const data = await response.json();
            addMessage("bot", data.reply);
        } catch {
            addMessage("bot", "Không kết nối được với chatbot. Bạn thử lại nhé.");
        } finally {
            sendButton.disabled = false;
            sendButton.textContent = "Gửi";
        }
    });

    toggle.addEventListener("click", function() {
        setOpen(panel.hidden);
    });

    closeButton.addEventListener("click", function() {
        setOpen(false);
    });

    panel.addEventListener("keydown", function(event) {
        if (event.key === "Escape") {
            setOpen(false);
        }
    });
})();