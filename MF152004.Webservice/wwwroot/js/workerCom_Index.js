﻿var connection = new signalR.HubConnectionBuilder().withUrl('/workerWebCom').build();
var labelPrinterModal;
var successToast;
var modalIsOpen = false;

setTimeout(checkLabelCookie, 2000); //to check if cookie already exists

connection.on("ReceiveStatus", function (status) {

    if (status.currentStatus === 1) {

        if (getCookie("labelprinter") == "") {
            popUpLabelprinterError(status.message, status.transportReference, status.readedCodes);
        }
    }
});

connection.on("ReceiveMqttStatus", function (status) {

    document.getElementById('statusOnline').hidden = !status;
    document.getElementById('statusOffline').hidden = status;
});

connection.on("ReceiveDestinationStatus", function (destinationsStatus) {

    if (destinationsStatus != null && destinationsStatus.destinations != null) {

        var dest = destinationsStatus.destinations.find((destination) => destination.id == $('#GateId').val());
        var activeStatus = "";

        if (dest.active) {
            activeStatus = "Zielstatus: <span class='text-success'>Aktiv - kann avisiert werden</span>";
        }
        else {
            activeStatus = "Zielstatus: <span class='text-warning'>Inaktiv - kann nicht avisiert werden</span>";
        }

        $('#gateStatus').empty();
        $('#gateStatus').append(activeStatus);
    }

});

connection.onclose(function () {

    setTimeout(function () {
        startConnection();
    }, 5000);

});

startConnection();

async function startConnection() {
    try {
        await connection.start();
        console.log("Connected to Hub");
    } catch (err) {
        console.log(err);
        setTimeout(startConnection, 5000);
    }
}

async function confirmMatchError() {

    const systemStatus = {
        currentStatus: 2,
        message: "",
        release: true,
        transportReference: $("#transportRef").text()
    };

    try {
        await connection.invoke("SendStatus", systemStatus);

        if (labelPrinterModal != null)
            labelPrinterModal.closeModal();

        if (getCookie("labelprinter") == "")
            deleteCookie("labelprinter");
    } catch (err) {
        console.error(err);
        toast = new bootstrap.Toast(document.getElementById('ackFailureToast'), { delay: 5000 });
        toast.show();
    }
}

async function confirmMatchError2() {

    const systemStatus = {
        currentStatus: 2,
        release: true,
    };

    try {
        await connection.invoke("SendStatus", systemStatus);

        toast = new bootstrap.Toast(document.getElementById('ackSuccessToast'), { delay: 5000 });
        toast.show();

    } catch (err) {
        console.error(err);

        toast = new bootstrap.Toast(document.getElementById('ackFailureToast'), { delay: 5000 });
        toast.show();
    }
}

function popUpLabelprinterError(msg, transport, readedCodes) {

    var splittedMsg = msg.split("#");
    var ol = "<ol>";
    splittedMsg.forEach(li => {
        ol += "<li>" + li + "</li>";
    })

    ol += "</ol>";

    $('#labelPrinterMsg').empty();
    $('#labelPrinterMsg').append(ol);
    $('#transportRef').text(transport);
    $('#receivedCodes').text(readedCodes);

    if (getCookie("labelprinter") == "") {
        setCookie("labelprinter", msg + "_" + transport + "_" + readedCodes, 14);
    }

    labelPrinterModal = new bootstrap.Modal(document.getElementById('labelPrinterModal'), { backdrop: 'static', keyboard: false });
    labelPrinterModal.show();
    modalIsOpen = true;
}

function closeModal() {
    modalIsOpen = false;
    setTimeout(checkLabelCookie, 2000);
}

function checkLabelCookie() {

    let cookie = getCookie("labelprinter");

    if (cookie != "" && !modalIsOpen) {

        var splittedCookie = cookie.split("_");

        popUpLabelprinterError(splittedCookie[0], splittedCookie[1], splittedCookie[2]);
    }
}


//popUpLabelprinterError("Paket nach der Prüfung weiterschieben#Druckdaten an den Etikettierern löschen", "555", "1928");