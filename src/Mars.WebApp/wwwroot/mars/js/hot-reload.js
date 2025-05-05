/// <reference path="./signalr.js" />


window.addEventListener("load", (event) => {
    /** @type {import('./HubConnection').HubConnection} */
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/_ws/ws")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    async function start() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.log(err);
            setTimeout(start, 5000);
        }
    };

    connection.onclose(async () => {
        await start();
    });

    connection.on('reload', ()=>{
        location.reload();
    })

    connection.on('refreshcss', (filename)=>{
        refreshCSS(filename);
    })


    // Start the connection.
    start();
});

function refreshCSS(changed_filename) {
    var sheets = [].slice.call(document.getElementsByTagName("link"));
    var head = document.getElementsByTagName("head")[0];
    for (var i = 0; i < sheets.length; ++i) {
        var elem = sheets[i];
        head.removeChild(elem);
        var rel = elem.rel;
        if (elem.href && typeof rel != "string" || rel.length == 0 || rel.toLowerCase() == "stylesheet") {
            var url = elem.href.replace(/(&|\?)_cacheOverride=\d+/, '');
            const pathname = new URL(url).pathname;
            var filename = pathname.split('/').pop();
            if(filename == changed_filename){
                elem.href = url + (url.indexOf('?') >= 0 ? '&' : '?') + '_cacheOverride=' + (new Date().valueOf());
            }
        }
        head.appendChild(elem);
    }
}