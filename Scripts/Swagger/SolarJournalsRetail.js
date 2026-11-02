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

    function readData(path) {
        return fetch(root + path, { headers: { Accept: "application/json" } }).then(function (response) {
            return response.json().then(function (payload) {
                var error = payload.errorMessage || (payload.data && payload.data.ErrorMessage);
                if (!response.ok || error || payload.data == null)
                    throw new Error(error || payload.Message || "Unable to load report choices.");
                return payload.data;
            });
        });
    }

    function bindForm(form) {
        if (form.dataset.solarJournalsRetail) return;
        var operation = form.closest(".operation");
        var path = operation && operation.querySelector(".path");
        var body = form.querySelector('textarea[name="request"]');
        if (!path || !/\/api\/pucsl\/solarJournalsRetail\/?$/i.test(path.textContent.trim()) || !body) return;
        var originalRow = body.closest("tr");
        if (!originalRow) return;
        form.dataset.solarJournalsRetail = "true";

        function field(name, title, description) {
            var row = document.createElement("tr");
            var labelCell = document.createElement("td");
            var label = document.createElement("label");
            var select = document.createElement("select");
            select.name = name;
            select.id = body.id + "-" + name;
            select.style.minWidth = "280px";
            select.setAttribute("aria-label", title);
            label.htmlFor = select.id;
            label.textContent = title;
            labelCell.appendChild(label);
            row.appendChild(labelCell);
            var valueCell = document.createElement("td");
            valueCell.appendChild(select);
            var status = document.createElement("div");
            status.setAttribute("role", "status");
            status.style.marginTop = "4px";
            valueCell.appendChild(status);
            row.appendChild(valueCell);
            [description, "body field", "string"].forEach(function (text) {
                var cell = document.createElement("td");
                cell.textContent = text;
                row.appendChild(cell);
            });
            originalRow.parentNode.insertBefore(row, originalRow);
            return { select: select, status: status, label: label };
        }

        var typeField = field("areaType", "Area Type", "Choose Area, Province, Division, or Entire CEB.");
        var locationField = field("locationCode", "Location", "Required except for Entire CEB. Choices depend on Area Type.");
        var cycleField = field("billCycle", "Bill Cycle", "Choose an available bill cycle. The latest cycle is selected initially.");
        var areaType = typeField.select;
        var location = locationField.select;
        var cycle = cycleField.select;
        ["Area", "Province", "Division", "Entire CEB"].forEach(function (type) { option(areaType, type, type); });
        areaType.required = true;
        cycle.required = true;
        // Swagger still serializes this textarea as the documented JSON POST body.
        originalRow.style.display = "none";
        var locationRequestId = 0;

        function message(field, text, error) {
            field.status.textContent = text;
            field.status.style.color = error ? "#b00020" : "";
        }

        function syncBody() {
            body.value = JSON.stringify({
                AreaType: areaType.value,
                LocationCode: areaType.value === "Entire CEB" ? "" : location.value,
                BillCycle: cycle.value
            });
        }

        function displayLocations(type, rows) {
            location.textContent = "";
            option(location, "", "Select " + type);
            rows.forEach(function (row) {
                var code = row.AreaCode || row.ProvinceCode || row.RegionCode;
                var name = row.AreaName || row.ProvinceName;
                if (code) option(location, code, name ? code + " - " + name : code);
            });
            location.disabled = location.options.length === 1;
            message(locationField, location.disabled ? "No locations are available. Select Area Type again to retry." : "", location.disabled);
            syncBody();
        }

        function loadLocations() {
            var type = areaType.value;
            var currentRequest = ++locationRequestId;
            location.textContent = "";
            location.disabled = true;
            location.required = type !== "Entire CEB";
            location.classList.remove("error");
            locationField.label.textContent = type === "Entire CEB" ? "Location" : type;
            if (type === "Entire CEB") {
                option(location, "", "All CEB locations");
                message(locationField, "No location selection is needed for Entire CEB.", false);
                syncBody();
                return;
            }
            if (locationCache[type]) {
                displayLocations(type, locationCache[type]);
                return;
            }
            option(location, "", "Loading locations...");
            syncBody();
            message(locationField, "Loading locations...", false);
            var endpoint = type === "Area" ? "areas" : type === "Province" ? "province" : "region";
            readData("/api/ordinary/" + endpoint).then(function (rows) {
                if (!Array.isArray(rows)) throw new Error("Invalid location choices.");
                var rowError = rows.find(function (row) { return row.ErrorMessage; });
                if (rowError) throw new Error(rowError.ErrorMessage);
                rows.sort(function (a, b) {
                    return (a.AreaName || a.ProvinceName || a.RegionCode || "").localeCompare(b.AreaName || b.ProvinceName || b.RegionCode || "");
                });
                locationCache[type] = rows;
                if (currentRequest === locationRequestId) displayLocations(type, rows);
            }).catch(function (error) {
                if (currentRequest !== locationRequestId) return;
                location.textContent = "";
                option(location, "", "Unable to load locations");
                message(locationField, error.message + " Select Area Type again to retry.", true);
                syncBody();
            });
        }

        function loadCycles() {
            cycle.textContent = "";
            cycle.disabled = true;
            option(cycle, "", "Loading bill cycles...");
            message(cycleField, "Loading bill cycles...", false);
            readData("/api/ordinary/areas/billcycle/min").then(function (data) {
                var max = Number(data.MaxBillCycle);
                if (!Number.isInteger(max) || max <= 0 || !Array.isArray(data.BillCycles) || !data.BillCycles.length)
                    throw new Error("No bill cycles are available.");
                cycle.textContent = "";
                data.BillCycles.forEach(function (label, index) {
                    option(cycle, String(max - index), (max - index) + " - " + label);
                });
                cycle.disabled = false;
                message(cycleField, "", false);
                syncBody();
            }).catch(function (error) {
                cycle.textContent = "";
                option(cycle, "", "Unable to load bill cycles");
                message(cycleField, error.message, true);
                var retry = document.createElement("button");
                retry.type = "button";
                retry.textContent = "Retry";
                retry.addEventListener("click", loadCycles);
                cycleField.status.appendChild(document.createTextNode(" "));
                cycleField.status.appendChild(retry);
                syncBody();
            });
        }

        function validate(event) {
            syncBody();
            var locationInvalid = areaType.value !== "Entire CEB" && (location.disabled || !location.value);
            var cycleInvalid = cycle.disabled || !cycle.value;
            if (!locationInvalid && !cycleInvalid) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            if (locationInvalid) {
                location.classList.add("error");
                message(locationField, "Select a " + areaType.value.toLowerCase() + " before trying the report.", true);
                if (!location.disabled) location.focus();
            }
            if (cycleInvalid) message(cycleField, "Select an available bill cycle before trying the report.", true);
        }

        areaType.addEventListener("change", loadLocations);
        location.addEventListener("change", function () {
            location.classList.remove("error");
            message(locationField, "", false);
            syncBody();
        });
        cycle.addEventListener("change", syncBody);
        form.addEventListener("submit", validate, true);
        form.addEventListener("click", function (event) {
            if (event.target.closest(".submit")) validate(event);
        }, true);
        loadLocations();
        loadCycles();
    }

    function scan() { document.querySelectorAll("form.sandbox").forEach(bindForm); }
    scan();
    var scheduled = false;
    new MutationObserver(function () {
        if (scheduled) return;
        scheduled = true;
        setTimeout(function () { scheduled = false; scan(); }, 0);
    }).observe(document.getElementById("swagger-ui-container") || document.body, { childList: true, subtree: true });
})();
