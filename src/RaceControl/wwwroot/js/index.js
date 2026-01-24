const panel = new Panel('flag-panel');

const sessionHub = new signalR.HubConnectionBuilder()
    .withUrl('/session')
    .build();

const trackStatusHub = new signalR.HubConnectionBuilder()
    .withUrl('/track-status')
    .build();

let latency = 0;

sessionHub.on('CategoryChange', (category) => {
   console.log(category); 
});

trackStatusHub.on('FlagChange', (flagData) => {
    console.log(flagData);
});

const start = async () => {
    await sessionHub.start();
    await trackStatusHub.start();
    
    await sessionHub.invoke('CurrentSession');
}

await start();