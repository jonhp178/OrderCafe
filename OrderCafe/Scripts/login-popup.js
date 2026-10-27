(function () {
    "use strict";

    function initializeLoginPopup() {
        var popup = document.getElementById("loginPopup");
        var closeButton = document.getElementById("closeLoginPopup");

        if (
            typeof HTMLDialogElement === "undefined" ||
            !(popup instanceof HTMLDialogElement)
        ) {
            return;
        }

        function openPopup() {
            if (!popup.open) {
                popup.showModal();
            }
        }

        if (closeButton) {
            closeButton.addEventListener("click", function () {
                popup.close();
            });
        }

        // Nút mở popup trên Header.
        document.querySelectorAll("[data-open-login]").forEach(function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();
                openPopup();
            });
        });

        // Giá trị này do Layout xác định từ Session và trang hiện tại.
        if (popup.dataset.autoOpen === "true") {
            openPopup();
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeLoginPopup);
    } else {
        initializeLoginPopup();
    }
})();