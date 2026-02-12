const panel = new Panel('flag-panel');

const sessionHub = new signalR.HubConnectionBuilder()
    .withUrl('/session')
    .build();

const trackStatusHub = new signalR.HubConnectionBuilder()
    .withUrl('/track-status')
    .build();

let latency = 0;

sessionHub.on('CategoryChange', (category) => {
   if (category === null)
       return;
   
   latency = category.latency * 1000;
   console.log(latency);
});

trackStatusHub.on('FlagChange', (flagData) => {
    if (flagData === null)
        return;
    
    setTimeout(() => {
        panel.setFlag(flagData.flag, flagData?.driver);
    }, latency)
});

const start = async () => {
    await sessionHub.start();
    await trackStatusHub.start();
    
    await sessionHub.invoke('CurrentSession');
}

start();