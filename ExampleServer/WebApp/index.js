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
        url: '/testurl',
        type: 'GET',
        dataType: 'text/plain',
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log("Succsess");
                default:
                    console.log("Status get " + xhr.status);
                    break;
            }
        }
    });
}

function postTest() {
    $.ajax({
        url: '/testurl',
        type: 'POST',
        contentType: 'text/plain; charset=utf-8',
        data: 'data fra test post',
        complete: function (xhr) {
            switch (xhr.status) {
                case 200:
                    console.log("Succsess");
                default:
                    console.log("Status post " + xhr.status);
                    break;
            }
        }
    });
}