let connection;
let pingId;
$(function () {
    $("#createSimulation").on("click", function () {
        var floors = $("#floors").val();
        var elevators = $("#elevators").val();
        var capacity = $("#capacity").val();
        var people = $("#people").val();
        var timeout = $("#timeout").val();
        connection = new signalR.HubConnectionBuilder()
            .withUrl(`/hub?f=${floors}&e=${elevators}&c=${capacity}&p=${people}&t=${timeout}`)
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on("Connected", (simulationId) => {
            $("#createSim")[0].style.display = "none";
            $("#interactionForms")[0].style.display = "block";

            for (const action of document.querySelectorAll(".requestForm")) {

                action.action = action.action.replace("~setbyjs~", simulationId);
                action.style.display = "block";
            }
            setPingInterval();
            console.log("Connected : " + simulationId);
        });

        connection.on("Event", (type, values) => {

            $("#simulationResponse")[0].innerHTML =
                `<p>${type} - ${Object.values(values).join(", ")}</p>`
                + $("#simulationResponse")[0].innerHTML;
        });

        connection.onclose(() => {
            alert("Connection Closed");
            clearInterval(pingId);
        });

        // Start the connection.
        start();
    });

    for (const action of document.querySelectorAll(".actionForm")) {
        action.addEventListener("click", function (e) {
            post(document.getElementById(this.dataset.formId));
        });
    }

    function post(form) {

        $("input[name='RequestTime'", form).val(new Date().toISOString());

        return $.ajax({
            type: "POST",
            url: form.action,
            data: $(form).serialize(),
            dataType: 'json'
        });
    }

    function get(url) {

        return $.ajax({
            type: "get",
            url: url,
            dataType: 'json'
        });
    }
    function setPingInterval() {
        var summary = $("#simulationSummary")[0];
        var body = $("#simBody")[0];

        summary.style.width = "15%";
        summary.style.display = "inline-block";

        body.style.width = "75%";
        body.style.display = "inline-block";
        body.style.position = "absolute";
        body.style.right = "100px";
        ping();
        pingId = setInterval(ping, 3000);
    }
    function ping() {
        var summary = $("#simulationSummary")[0];

        return get(summary.action)
            .done((data) => {
                summary.innerHTML = Object.entries(data).map(([key, value]) => `<p>${key} : ${value}</p>`).join("");
            });

    }

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");

        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };
});


