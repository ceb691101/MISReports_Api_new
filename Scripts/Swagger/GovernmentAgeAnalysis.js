(function () {
    "use strict";

    var root = window.swashbuckleConfig && window.swashbuckleConfig.rootUrl;
    root = (root || window.location.href.split("/swagger/")[0]).replace(/\/$/, "");
    var locationCache = {};

    function option(select, value, label) {
        var item = document.createElement("option");
        item.value = value;
        item.textContent = label;
        select.appendChild(item);
    }

    function bindForm(form) {
        if (form.dataset.governmentAgeLocations) return;
        var areaType = form.querySelector('[name="areaType"]');
        var input = form.querySelector('[name="locationCode"]');
        if (!areaType || !input || !form.querySelector('[name="reportType"]')) return;
        form.dataset.governmentAgeLocations = "true";

        var location = document.createElement("select");
        location.name = input.name;
        location.id = input.id;
        location.className = input.className.replace(/\brequired\b/g, "").trim();
        location.style.minWidth = "240px";
        input.parentNode.replaceChild(location, input);
        var status = document.createElement("div");
        status.setAttribute("role", "status");
        status.style.marginTop = "4px";
        location.parentNode.appendChild(status);
        var requestId = 0;

        function message(text, error) {
            status.textContent = text;
            status.style.color = error ? "#b00020" : "";
        }

        function displayLocations(type, data) {
            location.textContent = "";
            option(location, "", "Select " + type);
            data.forEach(function (item) {
                option(location, item.Code, item.Code + " - " + item.Name);
            });
            location.disabled = data.length === 0;
            message(data.length ? "Choose a " + type.toLowerCase() + " before trying the report."
                : "No " + type.toLowerCase() + " locations are available.", !data.length);
        }

        function loadLocations() {
            var type = areaType.value;
            var currentRequest = ++requestId;
            location.textContent = "";
            location.setAttribute("aria-label", type === "Entire CEB" ? "Location" : type);
            location.classList.remove("error");
            location.required = type !== "Entire CEB";
            location.disabled = true;
            if (type === "Entire CEB") {
                option(location, "", "All provinces and divisions");
                message("No location selection is needed for Entire CEB.", false);
                return;
            }
            if (locationCache[type]) {
                displayLocations(type, locationCache[type]);
                return;
            }
            option(location, "", "Loading " + (type === "Province" ? "provinces" : "divisions") + "...");
            message("Loading locations...", false);
            fetch(root + "/api/debtors/bulk/government-age-analysis/locations?areaType=" + encodeURIComponent(type), {
                headers: { Accept: "application/json" }
            }).then(function (response) {
                return response.json().then(function (payload) {
                    if (!response.ok || payload.errorMessage || !Array.isArray(payload.data))
                        throw new Error(payload.errorMessage || payload.Message || "Unable to load locations.");
                    return payload.data;
                });
            }).then(function (data) {
                locationCache[type] = data;
                if (currentRequest === requestId) displayLocations(type, data);
            }).catch(function (error) {
                if (currentRequest !== requestId) return;
                location.textContent = "";
                option(location, "", "Unable to load locations");
                message(error.message + " Select Area Type again to retry.", true);
            });
        }

        function validate(event) {
            if (areaType.value === "Entire CEB" || (!location.disabled && location.value)) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            location.classList.add("error");
            message(location.disabled ? "Wait for locations to load, or select Area Type again to retry."
                : "Select a " + areaType.value.toLowerCase() + " before trying the report.", true);
            if (!location.disabled) location.focus();
        }

        areaType.addEventListener("change", loadLocations);
        location.addEventListener("change", function () {
            location.classList.remove("error");
            message(location.value ? "" : "Choose a " + areaType.value.toLowerCase() + " before trying the report.", false);
        });
        form.addEventListener("submit", validate, true);
        form.addEventListener("click", function (event) {
            if (event.target.closest(".submit")) validate(event);
        }, true);
        loadLocations();
    }

    function scan() {
        document.querySelectorAll("form.sandbox").forEach(bindForm);
    }

    scan();
    var scheduled = false;
    new MutationObserver(function () {
        if (scheduled) return;
        scheduled = true;
        setTimeout(function () { scheduled = false; scan(); }, 0);
    }).observe(document.getElementById("swagger-ui-container") || document.body, { childList: true, subtree: true });
})();
