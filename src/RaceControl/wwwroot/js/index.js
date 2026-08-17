const panel = new Panel('flag-panel');

const raceControlHub = new signalR.HubConnectionBuilder()
    .withUrl('/signalr')
    .build();

let latency = 0;

raceControlHub.on('CategoryChange', (category) => {
   if (category === null)
       return;

   latency = category.latency * 1000;
});

raceControlHub.on('FlagChange', (flagData) => {
    if (flagData === null)
        return;

    setTimeout(() => {
        panel.setFlag(flagData.flag, flagData?.driver);
    }, latency)
});

const start = async () => {
    await raceControlHub.start();
}

start().then(r => {});