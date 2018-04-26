$(document).ready(function () {
    $("#idbtn1").on('click', function () {
        getTest();
    });
    $("#idbtn2").on('click', function () {
        postTest();
    });
});

function getTest() {
    $.ajax({
        url: 'jsonobject',
        type: 'GET',
        dataType: 'application/json; charset=utf-8',
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log(JSON.parse(xhr.responseText));
                default:
                    console.log("Status get " + xhr.status);
                    break;
            }
        }
    });
}

function postTest() {
    $.ajax({
        url: 'testurl',
        type: 'POST',
        contentType: 'text/plain; charset=utf-8',
        data: "Text from the index.js file",
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log(xhr.responseText);
                default:
                    console.log("Status post " + xhr.status);
                    break;
            }
        }
    });
}