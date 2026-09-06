const panel = new Panel('flag-panel');

const raceControlHub = new signalR.HubConnectionBuilder()
    .withUrl('/signalr')
    .build();

let latency = 0;

raceControlHub.on('FlagChange', (flagData) => {
    if (flagData === null)
        return;

    panel.setFlag(flagData.flag, flagData?.driver);
});

const start = async () => {
    await raceControlHub.start();
}

start().then(r => {});