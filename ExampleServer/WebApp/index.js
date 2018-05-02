$(document).ready(function () {
    $("#idbtn1").on('click', function () {
        getTest($("#inputURL").val());
    });
    $("#idbtn12").on('click', function () {
        getTestJSON($("#inputURL").val());
    });
    $("#idbtn2").on('click', function () {
        postTest($("#inputURL").val(), $("#inputData").val());
    });
});

function getTest(url) {
    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'application/json; charset=utf-8',
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log(xhr.responseText);
                    break;
                default:
                    console.log("Status get " + xhr.status);
                    break;
            }
        }
    });
}

function getTestJSON(url) {
    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'application/json; charset=utf-8',
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log(JSON.parse(xhr.responseText));
                    break;
                default:
                    console.log("Status get " + xhr.status);
                    break;
            }
        }
    });
}

function postTest(url, data) {
    $.ajax({
        url: url,
        type: 'POST',
        contentType: 'text/plain; charset=utf-8',
        data: data,
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log(xhr.responseText);
                    break;
                default:
                    console.log("Status post " + xhr.status);
                    break;
            }
        }
    });
}